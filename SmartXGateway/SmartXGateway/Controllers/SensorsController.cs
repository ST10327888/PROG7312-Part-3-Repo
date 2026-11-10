using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartXGateway.Data;
using SmartXGateway.Hubs;
using SmartXGateway.Models;
using SmartXGateway.Services;

namespace SmartXGateway.Controllers
{
    [AutoValidateAntiforgeryToken]
    [Authorize]
    public class SensorsController : Controller
    {
        private readonly SensorStore _store;
        private readonly DeploymentValidator _validator;
        private readonly FileEncryptionService _crypto;
        private readonly IHubContext<TelemetryHub> _hub;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SensorsController> _logger;

        private const int MaxUploadBytes = 5 * 1024 * 1024;   // 5 MB
        private const int MinBurst = 1;
        private const int MaxBurst = 500;

        public SensorsController(
            SensorStore store,
            DeploymentValidator validator,
            FileEncryptionService crypto,
            IHubContext<TelemetryHub> hub,
            IWebHostEnvironment env,
            ILogger<SensorsController> logger)
        {
            _store = store;
            _validator = validator;
            _crypto = crypto;
            _hub = hub;
            _env = env;
            _logger = logger;
        }

        public IActionResult Index() => View(_store.AllSensors);

        [HttpGet]
        public IActionResult Register() => View(new SensorRegistrationViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(SensorRegistrationViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            if (vm.Attachment != null && vm.Attachment.Length > MaxUploadBytes)
            {
                ModelState.AddModelError(nameof(vm.Attachment),
                    $"Attachment must be at most {MaxUploadBytes / (1024 * 1024)} MB.");
                return View(vm);
            }

            var mac = vm.MacAddress.Trim().ToUpperInvariant();

            if (_store.Sensors.Contains(mac))
            {
                ModelState.AddModelError(nameof(vm.MacAddress), "A sensor with this MAC already exists.");
                return View(vm);
            }

            string? savedPath = null;
            try
            {
                if (vm.Attachment != null && vm.Attachment.Length > 0)
                {
                    var uploads = Path.Combine(_env.WebRootPath, "uploads");
                    await using var stream = vm.Attachment.OpenReadStream();
                    savedPath = await _crypto.EncryptAndSaveAsync(stream, vm.Attachment.FileName, uploads);
                }

                var sensor = new Sensor
                {
                    MacAddress = mac,
                    Location = vm.Location,
                    Category = vm.Category,
                    LastValue = 0,
                    AttachmentPath = savedPath
                };

                if (_store.Sensors.Add(sensor))
                {
                    _store.DeviceLookup.GetOrCreate(sensor.MacAddress, sensor.Location, sensor.Category);
                    _store.Log($"Registered sensor {sensor.MacAddress} at {sensor.Location}.", "success");
                    TempData["SuccessMessage"] = $"Sensor {sensor.MacAddress} and its deployment location were added successfully.";
                    _logger.LogInformation("Registered sensor {Mac}", sensor.MacAddress);
                }
                else
                {
                    TryDeleteUpload(savedPath);
                    _store.Log($"Failed to register {sensor.MacAddress}.", "danger");
                    _logger.LogWarning("Registration race for {Mac}", sensor.MacAddress);
                }
            }
            catch (Exception ex)
            {
                TryDeleteUpload(savedPath);
                _logger.LogError(ex, "Registration failed for {Mac}", mac);
                ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                return View(vm);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(string mac)
        {
            if (string.IsNullOrWhiteSpace(mac)) return NotFound();

            var s = _store.Get(mac);
            if (s?.AttachmentPath == null) return NotFound();

            var fullPath = Path.Combine(_env.WebRootPath, s.AttachmentPath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            try
            {
                var bytes = await _crypto.DecryptAsync(fullPath);
                var downloadName = Path.GetFileName(s.AttachmentPath).Replace(".enc", "");
                return File(bytes, "application/octet-stream", "decrypted_" + downloadName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Decryption failed for {Mac}", mac);
                return NotFound();
            }
        }

        public IActionResult Telemetry()
        {
            ViewBag.Sensors = _store.AllSensors;
            ViewBag.History = _store.TelemetryHistory.TakeLast(50).Reverse().ToList();
            ViewBag.Issues = _store.ReportedIssues.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> IngestTelemetry([FromBody] TelemetryPacket<double> packet)
        {
            if (packet == null || string.IsNullOrWhiteSpace(packet.DeviceMac))
                return BadRequest(new { ok = false, message = "Invalid packet." });

            var sensor = _store.Get(packet.DeviceMac);
            if (sensor == null)
                return NotFound(new { ok = false, message = "Unknown sensor." });

            packet.Timestamp = DateTime.UtcNow;
            packet.Priority = Math.Clamp(packet.Priority, 0, 10);

            sensor.LastValue = packet.Value;
            _store.TelemetryHistory.Add(packet);
            _store.BufferReading(packet.DeviceMac, packet.Value);

            var dev = _store.DeviceLookup.GetOrCreate(sensor.MacAddress, sensor.Location, sensor.Category);
            _store.DeviceLookup.TryUpdate(sensor.MacAddress, packet.Value);

            bool isAnomaly = packet.Value > 1000 || packet.Value < 0;
            string level = isAnomaly ? "warning" : "info";

            if (isAnomaly)
            {
                bool firstTime = _store.DeviceLookup.RegisterAlert(
                    packet.DeviceMac, AlertKind.Anomaly, "Anomaly spike");

                if (firstTime)
                    _store.Log($"⚠ New unique alert: {packet.DeviceMac} = {packet.Value}", "warning");
                else
                    _store.Log($"⚠ Ongoing anomaly: {packet.DeviceMac} = {packet.Value}", "info");
            }
            else
            {
                _store.DeviceLookup.ClearAlert(packet.DeviceMac, AlertKind.Anomaly);
                _store.Log($"Ingested {packet.DeviceMac} = {packet.Value}", "info");
            }

            _store.Predictive.Evaluate(dev, packet, sensor.Category.ToString());
            bool wentToPriority = _store.CommandStream.Enqueue(packet);
            _store.Timeline.Record(packet, isAnomaly);

            await _hub.Clients.All.SendAsync("packetReceived", new
            {
                mac = packet.DeviceMac,
                value = packet.Value,
                timestamp = packet.Timestamp,
                level,
                prioritized = wentToPriority,
                total = _store.TelemetryHistory.Count
            });

            return Ok(new
            {
                ok = true,
                mac = packet.DeviceMac,
                value = packet.Value,
                level,
                prioritized = wentToPriority,
                total = _store.TelemetryHistory.Count
            });
        }

        [HttpPost]
        public async Task<IActionResult> SimulateBurst(int count = 50)
        {
            count = Math.Clamp(count, MinBurst, MaxBurst);

            var rnd = new Random();
            var sensors = _store.AllSensors.ToList();
            if (sensors.Count == 0)
                return BadRequest(new { ok = false, message = "No sensors." });

            int anomaliesCreated = 0;

            for (int i = 0; i < count; i++)
            {
                var s = sensors[rnd.Next(sensors.Count)];
                double v = s.Category switch
                {
                    SensorCategory.Environmental => Math.Round(rnd.NextDouble() * 100, 2),
                    SensorCategory.PowerConsumption => Math.Round(rnd.NextDouble() * 2500, 2),
                    SensorCategory.Actuator => rnd.Next(0, 2),
                    _ => Math.Round(rnd.NextDouble() * 100, 2)
                };

                int priority = rnd.Next(0, 20) == 0 ? 10 : 0;
                var packet = new TelemetryPacket<double>(s.MacAddress, "sim", v, priority)
                {
                    Timestamp = DateTime.UtcNow
                };

                s.LastValue = packet.Value;
                _store.TelemetryHistory.Add(packet);
                _store.BufferReading(s.MacAddress, v);

                var dev = _store.DeviceLookup.GetOrCreate(s.MacAddress, s.Location, s.Category);
                _store.DeviceLookup.TryUpdate(s.MacAddress, v);

                bool isAnomaly = v > 1000 || v < 0;
                if (isAnomaly)
                {
                    _store.DeviceLookup.RegisterAlert(s.MacAddress, AlertKind.Anomaly, "Anomaly spike");
                    anomaliesCreated++;
                }
                else
                {
                    _store.DeviceLookup.ClearAlert(s.MacAddress, AlertKind.Anomaly);
                }

                _store.Predictive.Evaluate(dev, packet, s.Category.ToString());
                _store.CommandStream.Enqueue(packet);
                _store.Timeline.Record(packet, isAnomaly);
            }

            // ✅ FIX: Check for devices that haven't been seen recently
            _store.CheckDisconnectedDevices(TimeSpan.FromMinutes(2));

            _store.Log($"Simulated burst of {count} packets ({anomaliesCreated} anomalies).", "success");
            await _hub.Clients.All.SendAsync("burstComplete", new { total = _store.TelemetryHistory.Count });
            return Ok(new { ok = true, total = _store.TelemetryHistory.Count, anomalies = anomaliesCreated });
        }

        [HttpPost]
        public IActionResult Delete([FromBody] DeleteSensorRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Mac))
                return BadRequest(new { ok = false, message = "MAC required." });

            var mac = req.Mac.Trim().ToUpperInvariant();

            var sensor = _store.Get(mac);
            if (sensor == null)
                return Ok(new { ok = false, message = "Sensor not found." });

            if (!string.IsNullOrEmpty(sensor.AttachmentPath))
            {
                var fullPath = Path.Combine(_env.WebRootPath, sensor.AttachmentPath.TrimStart('/'));
                TryDeleteFile(fullPath);
            }

            _store.Sensors.Remove(mac);
            _store.DeviceLookup.RemoveDevice(mac);
            _store.Log($"🗑 Sensor {mac} removed.", "warning");
            _logger.LogInformation("Sensor {Mac} deleted by {User}", mac, User.Identity?.Name ?? "anon");

            return Ok(new { ok = true });
        }

        [HttpGet]
        public IActionResult Feed()
        {
            var snapshot = _store.ActivityFeed.Take(20).ToList();
            return Json(snapshot);
        }

        public IActionResult ValidateTree()
        {
            var tree = _validator.BuildSampleTree();
            var error = _validator.Validate(tree);
            ViewBag.Result = error ?? "Deployment tree is fully configured and valid.";
            ViewBag.IsValid = error == null;
            return View(tree);
        }

        [HttpGet]
        public IActionResult AggregateDemo(string mac1, string mac2)
        {
            var a = _store.Get(mac1);
            var b = _store.Get(mac2);
            if (a == null || b == null) return BadRequest("Unknown MAC.");
            Sensor combined = a + b;
            Sensor delta = a - b;
            return Json(new
            {
                combined = combined.LastValue,
                delta = delta.LastValue,
                combinedMac = combined.MacAddress
            });
        }

        private void TryDeleteUpload(string? webPath)
        {
            if (string.IsNullOrEmpty(webPath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, webPath.TrimStart('/'));
            TryDeleteFile(fullPath);
        }

        private void TryDeleteFile(string fullPath)
        {
            try
            {
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file {Path}", fullPath);
            }
        }

        public record DeleteSensorRequest(string Mac);
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Troelsen, A. and Japikse, P., 2017. Pro C# 7: With .NET and 
       .NET Core. 8th ed. New York: Apress.
       
   Wentworth, P., 2015. Think Sharply with C#. [Online] Available 
       at: http://www.ict.ru.ac.za/Resources/ThinkSharply/ThinkSharply/index.html [Accessed 18 November 2025].
   ============================================================ */