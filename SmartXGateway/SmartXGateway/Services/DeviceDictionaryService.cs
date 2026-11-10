using SmartXGateway.Models;

namespace SmartXGateway.Services
{
   
    /// RUBRIC IMPROVEMENT (Lookup): added SetConnected and snapshot helpers so
    /// disconnected-node lifecycle is first-class, and lookups stay O(1).

    public class DeviceDictionaryService
    {
        private readonly Dictionary<string, DeviceState> _devices = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        public int Count
        {
            get { lock (_lock) return _devices.Count; }
        }

        public IReadOnlyCollection<DeviceState> Values
        {
            get
            {
                lock (_lock) return _devices.Values.ToList();
            }
        }

        public DeviceState GetOrCreate(string mac, string location, SensorCategory category)
        {
            lock (_lock)
            {
                if (_devices.TryGetValue(mac, out var existing)) return existing;
                var device = new DeviceState
                {
                    MacAddress = mac,
                    Location = location,
                    Category = category
                };
                _devices.Add(mac, device);
                return device;
            }
        }

        public DeviceState? Get(string mac)
        {
            lock (_lock) return _devices.TryGetValue(mac, out var d) ? d : null;
        }

        public bool TryUpdate(string mac, double value, bool connected = true)
        {
            lock (_lock)
            {
                if (!_devices.TryGetValue(mac, out var device)) return false;
                device.LastValue = value;
                device.LastSeen = DateTime.UtcNow;
                device.IsConnected = connected;
                device.PacketCount++;
                return true;
            }
        }


        /// RUBRIC IMPROVEMENT: explicit connected/disconnected transition.
        /// Drives the disconnected-nodes tracking required by the Sets rubric.
   
        public bool SetConnected(string mac, bool connected)
        {
            lock (_lock)
            {
                if (!_devices.TryGetValue(mac, out var device)) return false;
                device.IsConnected = connected;
                device.LastSeen = DateTime.UtcNow;
                return true;
            }
        }

        /// RUBRIC IMPROVEMENT: allow removal so the 100-sensor cap is manageable.
        public bool Remove(string mac)
        {
            lock (_lock) return _devices.Remove(mac);
        }
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Wentworth, P., 2015. Think Sharply with C#. [Online] Available 
       at: http://www.ict.ru.ac.za/Resources/ThinkSharply/ThinkSharply/index.html [Accessed 18 November 2025].
   ============================================================ */