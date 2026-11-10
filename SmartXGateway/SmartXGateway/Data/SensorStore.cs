using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SmartXGateway.Models;
using SmartXGateway.Services;

namespace SmartXGateway.Data
{
    public class SensorStore
    {
        public SensorCollection Sensors { get; } = new();
        private readonly object _lock = new();

        public List<TelemetryPacket<double>> TelemetryHistory { get; } = new();
        public List<ActivityFeedItem> ActivityFeed { get; } = new();
        public List<UserReportedIssue> ReportedIssues { get; } = new();

        public CommandStreamService CommandStream { get; } = new();
        public DeviceLookupService DeviceLookup { get; } = new();
        public PredictiveEngineService Predictive { get; } = new();
        public TimelineService Timeline { get; } = new();

        public Dictionary<string, AvlTree> TelemetryTrees { get; } = new();
        public MeshNetworkService MeshNetwork { get; set; } = new();
        private readonly object _treeLock = new();

        private readonly double[][] _rawTelemetryBuffer;
        private readonly int[] _rowFillCounts;
        private const int MaxSensors = 100;
        private const int ReadingsPerSensor = 50;

        public SensorStore()
        {
            _rawTelemetryBuffer = new double[MaxSensors][];
            _rowFillCounts = new int[MaxSensors];
            for (int i = 0; i < MaxSensors; i++)
                _rawTelemetryBuffer[i] = new double[ReadingsPerSensor];
        }

        public IReadOnlyCollection<Sensor> AllSensors => Sensors.ToList();
        public Sensor? Get(string mac) => Sensors[mac];

        public void BufferReading(string mac, double value)
        {
            int rowIndex = System.Math.Abs(mac.GetHashCode()) % MaxSensors;
            lock (_lock)
            {
                if (_rowFillCounts[rowIndex] >= ReadingsPerSensor)
                    PromoteRowToHistory(rowIndex, mac);
                _rawTelemetryBuffer[rowIndex][_rowFillCounts[rowIndex]++] = value;
            }
            RecordTelemetryInTree(mac, System.DateTime.UtcNow, value);
        }

        public void RecordTelemetryInTree(string mac, System.DateTime timestamp, double value)
        {
            lock (_treeLock)
            {
                if (!TelemetryTrees.ContainsKey(mac))
                    TelemetryTrees[mac] = new AvlTree();

                TelemetryTrees[mac].Insert(timestamp, value);
            }
        }

        public List<(System.DateTime Timestamp, double Value)> GetTelemetryRange(string mac, System.DateTime start, System.DateTime end)
        {
            lock (_treeLock)
            {
                if (TelemetryTrees.ContainsKey(mac))
                    return TelemetryTrees[mac].GetRange(start, end);
                return new List<(System.DateTime, double)>();
            }
        }

        private void PromoteRowToHistory(int rowIndex, string mac)
        {
            var row = _rawTelemetryBuffer[rowIndex];
            for (int i = 0; i < _rowFillCounts[rowIndex]; i++)
                TelemetryHistory.Add(new TelemetryPacket<double>(mac, "batched", row[i]));
            _rowFillCounts[rowIndex] = 0;
        }

        public void Log(string message, string level = "info")
        {
            ActivityFeed.Insert(0, new ActivityFeedItem { Message = message, Level = level });
            if (ActivityFeed.Count > 100)
                ActivityFeed.RemoveAt(ActivityFeed.Count - 1);
        }

        public int NextIssueId => ReportedIssues.Count == 0 ? 1 : ReportedIssues.Max(i => i.Id) + 1;

        // ✅ FIX: Added method to check for disconnected devices
        public void CheckDisconnectedDevices(TimeSpan timeout)
        {
            var cutoff = DateTime.UtcNow - timeout;
            var staleDevices = DeviceLookup.AllDevices
                .Where(d => d.IsConnected && d.LastSeen < cutoff)
                .ToList();

            foreach (var device in staleDevices)
            {
                DeviceLookup.MarkDisconnected(device.MacAddress);
                Log($"⚠ Device {device.MacAddress} marked as disconnected (timeout).", "warning");
            }
        }

        public void SeedDefaults()
        {
            if (Sensors.Count > 0) return;

            var seed = new[]
            {
                new Sensor { MacAddress = "AA:BB:CC:00:00:01", Location = "Sub-Zone B -> Zone 1 -> Facility A", Category = SensorCategory.Environmental, LastValue = 42.5 },
                new Sensor { MacAddress = "AA:BB:CC:00:00:02", Location = "Sub-Zone B -> Zone 1 -> Facility A", Category = SensorCategory.PowerConsumption, LastValue = 230.0 },
                new Sensor { MacAddress = "AA:BB:CC:00:00:03", Location = "Sub-Zone C -> Zone 2 -> Facility A", Category = SensorCategory.Actuator, LastValue = 1.0 },
                new Sensor { MacAddress = "AA:BB:CC:00:00:04", Location = "Zone 3 -> Facility B", Category = SensorCategory.Environmental, LastValue = 17.8 },
                new Sensor { MacAddress = "AA:BB:CC:00:00:05", Location = "Zone 3 -> Facility B", Category = SensorCategory.PowerConsumption, LastValue = 480.0 },
            };

            foreach (var s in seed)
            {
                Sensors.Add(s);
                DeviceLookup.GetOrCreate(s.MacAddress, s.Location, s.Category);
                DeviceLookup.TryUpdate(s.MacAddress, s.LastValue);
            }
            Log("Seeded 5 default sensors.", "success");
        }
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Troelsen, A. and Japikse, P., 2017. Pro C# 7: With .NET and 
       .NET Core. 8th ed. New York: Apress.
   ============================================================ */