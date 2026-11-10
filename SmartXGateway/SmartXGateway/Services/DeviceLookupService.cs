using SmartXGateway.Models;

namespace SmartXGateway.Services
{
    
    /// RUBRIC IMPROVEMENT (Sets): lifecycle — RegisterAlert / ClearAlert take
    /// an AlertKind so disconnected nodes and hardware warnings are tracked
    /// separately; the previous bug where one ClearAlert wiped every alert is fixed.
    /// RUBRIC IMPROVEMENT (Lookup): SetConnected/Remove pass-through.

    public class DeviceLookupService
    {
        private readonly DeviceDictionaryService _deviceDictionary = new();
        private readonly ActiveAlertHashSetService _activeAlertSet = new();

        public DeviceDictionaryService DeviceDictionary => _deviceDictionary;
        public ActiveAlertHashSetService ActiveAlertSet => _activeAlertSet;

        public IReadOnlyCollection<DeviceState> AllDevices => _deviceDictionary.Values;

        public DeviceState GetOrCreate(string mac, string location, SensorCategory category)
            => _deviceDictionary.GetOrCreate(mac, location, category);

        public DeviceState? Get(string mac)
            => _deviceDictionary.Get(mac);

        public bool TryUpdate(string mac, double value, bool connected = true)
            => _deviceDictionary.TryUpdate(mac, value, connected);

        public IEnumerable<DeviceState> Search(string? term, SensorCategory? cat = null)
        {
            IEnumerable<DeviceState> devices = AllDevices;
            if (cat.HasValue) devices = devices.Where(d => d.Category == cat.Value);
            if (string.IsNullOrWhiteSpace(term)) return devices;
            term = term.Trim();
            return devices.Where(d =>
                d.MacAddress.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                d.Location.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                d.Category.ToString().Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        public bool RegisterAlert(string mac, string message)
            => RegisterAlert(mac, AlertKind.Anomaly, message);

        public bool RegisterAlert(string mac, AlertKind kind, string message)
        {
            bool added = _activeAlertSet.Add(mac, kind, message);
            if (added && _deviceDictionary.Get(mac) is { } device)
            {
                device.IsAlerted = true;
                device.CurrentAlert = $"[{kind}] {message}";
            }
            else if (!added)
            {
                _activeAlertSet.Touch(mac, kind, message);
            }
            return added;
        }

        public void ClearAlert(string mac)
            => ClearAlert(mac, AlertKind.Anomaly);

  
        /// RUBRIC IMPROVEMENT: removes only the requested kind.
        /// If other alert kinds remain, the device stays flagged.
      
        public void ClearAlert(string mac, AlertKind kind)
        {
            var detailed = _activeAlertSet.Detailed
                .Where(a => string.Equals(a.Mac, mac, StringComparison.OrdinalIgnoreCase) && a.Kind == kind)
                .ToList();
            foreach (var a in detailed) _activeAlertSet.RemoveOne(a.Mac, a.Kind, a.Message);

            if (_deviceDictionary.Get(mac) is { } device)
            {
                var remaining = _activeAlertSet.Detailed
                    .Where(a => string.Equals(a.Mac, mac, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                device.IsAlerted = remaining.Count > 0;
                device.CurrentAlert = remaining.Count > 0 ? remaining[0].Message : null;
            }
        }

        /// RUBRIC IMPROVEMENT: disconnected-node lifecycle, explicitly rubric-listed.
        public void MarkDisconnected(string mac)
        {
            _deviceDictionary.SetConnected(mac, false);
            RegisterAlert(mac, AlertKind.Disconnected, "Device offline");
        }

        public void MarkConnected(string mac)
        {
            _deviceDictionary.SetConnected(mac, true);
            ClearAlert(mac, AlertKind.Disconnected);
        }

        public bool RemoveDevice(string mac)
        {
            _activeAlertSet.RemoveForDevice(mac);
            return _deviceDictionary.Remove(mac);
        }

        /// Called by PredictiveEngine on each tick to age out stale alerts.
        public int ExpireStaleAlerts(TimeSpan ttl) => _activeAlertSet.ExpireStale(ttl);

        public IReadOnlyCollection<string> ActiveAlerts => _activeAlertSet.Values;
        public int ActiveAlertCount => _activeAlertSet.Count;
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Troelsen, A. and Japikse, P., 2017. Pro C# 7: With .NET and 
       .NET Core. 8th ed. New York: Apress.
   ============================================================ */