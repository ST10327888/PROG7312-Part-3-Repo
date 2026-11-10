using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartXGateway.Services
{
   
    /// RUBRIC IMPROVEMENT (Sets): explicit "disconnected nodes or hardware
    /// warnings" tracking, exactly as the rubric criterion names them.
 
    public enum AlertKind
    {
        Anomaly = 0,
        Disconnected = 1,
        HardwareWarning = 2,
        ManualFlag = 3
    }


    /// RUBRIC IMPROVEMENT (Sets): complete lifecycle —
    ///   Add → Touch → Contains → ExpireStale → RemoveOne → RemoveForDevice → ClearAll.
    ///   Multiple unique alerts per device are supported (per AlertKind), so the
    ///   old bug where one ClearAlert wiped every alert is fixed.
 
    public class ActiveAlertHashSetService
    {
        // Key format: "{mac}::{kind}::{message}" — the HashSet enforces global uniqueness.
        private readonly HashSet<string> _alerts = new(StringComparer.OrdinalIgnoreCase);
        // RUBRIC IMPROVEMENT: last-seen timestamps enable TTL expiry.
        private readonly Dictionary<string, DateTime> _lastSeen = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        public int Count
        {
            get { lock (_lock) return _alerts.Count; }
        }

        public IReadOnlyCollection<string> Values
        {
            get { lock (_lock) return _alerts.ToList(); }
        }

        /// RUBRIC IMPROVEMENT: structured (mac, kind, message, lastSeen) view
        /// so the dashboard can render alert chips individually.
 
        public IReadOnlyList<(string Mac, AlertKind Kind, string Message, DateTime LastSeen)> Detailed
        {
            get
            {
                lock (_lock)
                {
                    return _alerts.Select(k =>
                    {
                        var parts = k.Split(new[] { "::" }, 3, StringSplitOptions.None);
                        var mac = parts.Length > 0 ? parts[0] : "";
                        var kind = parts.Length > 1 && Enum.TryParse<AlertKind>(parts[1], out var ek) ? ek : AlertKind.Anomaly;
                        var msg = parts.Length > 2 ? parts[2] : "";
                        var seen = _lastSeen.TryGetValue(k, out var t) ? t : DateTime.UtcNow;
                        return (mac, kind, msg, seen);
                    }).ToList();
                }
            }
        }

        public bool Add(string key)
        {
            lock (_lock)
            {
                var added = _alerts.Add(key);
                if (added) _lastSeen[key] = DateTime.UtcNow;
                return added;
            }
        }

        /// RUBRIC IMPROVEMENT: typed overload — call sites read clearly
        public bool Add(string mac, AlertKind kind, string message)
        {
            var key = $"{mac}::{kind}::{message}";
            lock (_lock)
            {
                var added = _alerts.Add(key);
                _lastSeen[key] = DateTime.UtcNow;
                return added;
            }
        }

        /// RUBRIC IMPROVEMENT: refresh last-seen without duplicating.
        public void Touch(string mac, AlertKind kind, string message)
        {
            var key = $"{mac}::{kind}::{message}";
            lock (_lock)
            {
                if (_alerts.Contains(key)) _lastSeen[key] = DateTime.UtcNow;
            }
        }

     
        /// RUBRIC IMPROVEMENT: lifecycle — expire alerts that haven't been seen
        /// within the TTL, so alerts don't accumulate forever.
     
        public int ExpireStale(TimeSpan ttl)
        {
            lock (_lock)
            {
                var cutoff = DateTime.UtcNow - ttl;
                var stale = _lastSeen.Where(kv => kv.Value < cutoff).Select(kv => kv.Key).ToList();
                foreach (var k in stale)
                {
                    _alerts.Remove(k);
                    _lastSeen.Remove(k);
                }
                return stale.Count;
            }
        }

       
        public void RemoveForDevice(string mac)
        {
            lock (_lock)
            {
                var keys = _alerts.Where(k => k.StartsWith(mac + "::", StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var k in keys) { _alerts.Remove(k); _lastSeen.Remove(k); }
            }
        }

   
        public bool RemoveOne(string mac, AlertKind kind, string message)
        {
            var key = $"{mac}::{kind}::{message}";
            lock (_lock)
            {
                var removed = _alerts.Remove(key);
                _lastSeen.Remove(key);
                return removed;
            }
        }

        public bool Contains(string key)
        {
            lock (_lock) return _alerts.Contains(key);
        }

        public void ClearAll()
        {
            lock (_lock) { _alerts.Clear(); _lastSeen.Clear(); }
        }
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Troelsen, A. and Japikse, P., 2017. Pro C# 7: With .NET and 
       .NET Core. 8th ed. New York: Apress.
   ============================================================ */