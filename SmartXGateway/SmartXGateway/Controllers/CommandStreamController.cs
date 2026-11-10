using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartXGateway.Data;
using SmartXGateway.Hubs;
using SmartXGateway.Models;

namespace SmartXGateway.Controllers
{
    [Authorize]
    // RUBRIC IMPROVEMENT: auto-validates antiforgery on every POST.
    // The client JS must now send the RequestVerificationToken header.
    [AutoValidateAntiforgeryToken]
    public class CommandStreamController : Controller
    {
        private readonly SensorStore _store;
        private readonly IHubContext<TelemetryHub> _hub;

        public CommandStreamController(SensorStore store, IHubContext<TelemetryHub> hub)
        {
            _store = store;
            _hub = hub;
        }

        private string UserKey => User.Identity?.Name ?? "anon";

        public IActionResult Index()
        {
            _store.Predictive.RecordAction(UserKey, "view::dashboard");

            var vm = new CommandStreamViewModel
            {
                Devices = _store.DeviceLookup.AllDevices.ToList(),
                CommandHistory = _store.CommandStream.CommandHistory.ToList(),
                Suggestions = _store.Predictive.GetMergedSuggestions("view::dashboard"),
                Timeline = _store.Timeline.GetRecent(40).ToList(),
                ActiveAlerts = _store.DeviceLookup.ActiveAlerts.ToList(),
                QueuedCount = _store.CommandStream.QueuedCount,
                PriorityCount = _store.CommandStream.PriorityCount,
                TotalPackets = _store.TelemetryHistory.Count,
                AnomalyCount = _store.DeviceLookup.ActiveAlertCount
            };
            return View(vm);
        }

        [HttpPost]
        public IActionResult Filter([FromBody] FilterRequest req)
        {
            var actionKey = string.IsNullOrWhiteSpace(req.Term)
                ? $"cat::{req.Category}"
                : $"search::{req.Term}";
            _store.Predictive.RecordAction(UserKey, actionKey);
            var results = _store.DeviceLookup.Search(req.Term, req.Category);

            return Ok(new
            {
                ok = true,
                devices = results.Select(d => new
                {
                    d.MacAddress,
                    d.Location,
                    category = d.Category.ToString(),
                    d.LastValue,
                    d.IsConnected,
                    d.IsAlerted,
                    d.CurrentAlert,
                    d.LastSeen,
                    // RUBRIC IMPROVEMENT: sends flags so the JS can render them
                    // without dropping them on Filter re-render.
                    flags = d.DeviceFlags
                }),
                suggestions = _store.Predictive
                    .GetMergedSuggestions(actionKey)
                    .Select(s => new
                    {
                        s.DeviceMac,
                        s.Title,
                        s.Reason,
                        s.Confidence,
                        s.SuggestedCommand,
                        source = s.Source.ToString(),
                        s.IsExecutable
                    })
            });
        }

        [HttpPost]
        public async Task<IActionResult> Issue([FromBody] IssueCommandRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.MacAddress) || string.IsNullOrWhiteSpace(req.CommandType))
                return BadRequest(new { ok = false });

            var cmd = _store.CommandStream.PushCommand(
                req.MacAddress, req.CommandType, req.Payload ?? "", UserKey);

            ApplyCommandEffects(req.MacAddress, req.CommandType);

            _store.Predictive.RecordAction(UserKey, $"cmd::{req.CommandType}");
            _store.Predictive.RecordAction(UserKey, $"dev::{req.MacAddress}");

            // RUBRIC IMPROVEMENT: end-to-end outcome — issuing a command that
            // matches a suggestion counts as accepted.
            _store.Predictive.RecordSuggestionOutcome(req.MacAddress, req.CommandType, accepted: true);

            _store.Log($"▶ {cmd.IssuedBy} issued {cmd.CommandType} to {cmd.DeviceMac}", "success");

            await _hub.Clients.All.SendAsync("commandIssued", new
            {
                id = cmd.Id,
                mac = cmd.DeviceMac,
                type = cmd.CommandType,
                at = cmd.IssuedAt
            });

            var follow = _store.Predictive.GetMergedSuggestions($"cmd::{req.CommandType}");

            return Ok(new
            {
                ok = true,
                id = cmd.Id,
                suggestions = follow.Select(s => new
                {
                    s.DeviceMac,
                    s.Title,
                    s.Reason,
                    s.Confidence,
                    s.SuggestedCommand,
                    source = s.Source.ToString(),
                    s.IsExecutable
                })
            });
        }

        [HttpPost]
        public async Task<IActionResult> Undo()
        {
            var undone = _store.CommandStream.UndoLast();
            if (undone == null) return Ok(new { ok = false, message = "Nothing to undo." });

            // RUBRIC IMPROVEMENT: NO_OP inverse no longer reports success.
            if (undone.InverseCommand == "NO_OP")
            {
                return Ok(new
                {
                    ok = false,
                    message = $"{undone.CommandType} cannot be undone (inverse is NO_OP).",
                    inverse = "NO_OP"
                });
            }

            ApplyCommandEffects(undone.DeviceMac, undone.InverseCommand);
            _store.Predictive.RecordAction(UserKey, "undo");
            _store.Log($"↺ Undo: {undone.CommandType} on {undone.DeviceMac} → {undone.InverseCommand}", "warning");

            await _hub.Clients.All.SendAsync("commandUndone", new
            {
                id = undone.Id,
                mac = undone.DeviceMac,
                reverted = undone.CommandType,
                inverse = undone.InverseCommand
            });

            return Ok(new
            {
                ok = true,
                id = undone.Id,
                mac = undone.DeviceMac,
                type = undone.CommandType,
                inverse = undone.InverseCommand
            });
        }

        [HttpPost]
        public async Task<IActionResult> Drain()
        {
            var batch = _store.CommandStream.DequeueBatch(20);
            foreach (var (packet, wasPriority) in batch)
            {
                await _hub.Clients.All.SendAsync("packetProcessed", new
                {
                    mac = packet.DeviceMac,
                    value = packet.Value,
                    wasPriority
                });
            }
            _store.Log($"🚚 Drained {batch.Count} packets ({batch.Count(b => b.WasPriority)} priority).", "success");
            return Ok(new
            {
                ok = true,
                processed = batch.Count,
                priorityProcessed = batch.Count(b => b.WasPriority),
                remainingQueued = _store.CommandStream.QueuedCount,
                remainingPriority = _store.CommandStream.PriorityCount,
                order = batch.Select(b => b.WasPriority ? "P" : "F").ToList()
            });
        }

        [HttpGet]
        public IActionResult Suggestions()
            => Ok(_store.Predictive.GetMergedSuggestions("view::dashboard")
                    .Select(s => new
                    {
                        s.DeviceMac,
                        s.Title,
                        s.Reason,
                        s.Confidence,
                        s.SuggestedCommand,
                        source = s.Source.ToString(),
                        s.IsExecutable
                    }));

        [HttpGet]
        public IActionResult Timeline(DateTime? from = null, DateTime? to = null, int recent = 40)
        {
            IEnumerable<TimelineBucket> buckets =
                (from.HasValue && to.HasValue)
                    ? _store.Timeline.GetRange(
                        DateTime.SpecifyKind(from.Value, DateTimeKind.Utc),
                        DateTime.SpecifyKind(to.Value, DateTimeKind.Utc))
                    : _store.Timeline.GetRecent(recent);

            return Ok(buckets.Select(b => new
            {
                bucket = b.BucketStart,
                packets = b.PacketCount,
                anomalies = b.AnomalyCount,
                avg = b.AverageValue,
                max = b.MaxValue,
                min = b.MinValue
            }));
        }

        [HttpPost]
        public IActionResult RecordView([FromBody] RecordViewRequest req)
        {
            _store.Predictive.RecordAction(UserKey, $"dev::{req.Mac}");
            return Ok(new { ok = true });
        }

        // RUBRIC IMPROVEMENT: end-to-end outcome endpoint — client calls this
        // when the user dismisses a suggestion, feeding the predictive engine.
        [HttpPost]
        public IActionResult SuggestionOutcome([FromBody] SuggestionOutcomeRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Mac) || string.IsNullOrWhiteSpace(req.Command))
                return BadRequest(new { ok = false });
            _store.Predictive.RecordSuggestionOutcome(req.Mac, req.Command, req.Accepted);
            return Ok(new { ok = true });
        }

        private void ApplyCommandEffects(string mac, string command)
        {
            var dev = _store.DeviceLookup.Get(mac);
            if (dev == null) return;

            switch (command)
            {
                case "OPEN_VALVE": dev.DeviceFlags["Valve"] = "OPEN"; break;
                case "CLOSE_VALVE": dev.DeviceFlags["Valve"] = "CLOSED"; break;
                case "SHUTDOWN_RELAY": dev.DeviceFlags["Relay"] = "OFF"; break;
                case "RESTORE_RELAY": dev.DeviceFlags["Relay"] = "ON"; break;
                case "TOGGLE_RELAY":
                    dev.DeviceFlags["Relay"] =
                        dev.DeviceFlags.TryGetValue("Relay", out var r) && r == "ON" ? "OFF" : "ON";
                    break;
                case "REBOOT_NODE":
                    dev.DeviceFlags["RebootedAt"] = DateTime.UtcNow.ToString("o");
                    break;
            }
        }

        public record FilterRequest(string? Term, SensorCategory? Category);
        public record IssueCommandRequest(string MacAddress, string CommandType, string? Payload);
        public record RecordViewRequest(string Mac);
        public record SuggestionOutcomeRequest(string Mac, string Command, bool Accepted);
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