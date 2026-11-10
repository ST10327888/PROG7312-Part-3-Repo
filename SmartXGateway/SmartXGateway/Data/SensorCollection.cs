using System.Collections;
using System.Collections.Concurrent;
using SmartXGateway.Models;

namespace SmartXGateway.Data
{
   
    public class SensorCollection : IEnumerable<Sensor>
    {
        private readonly ConcurrentDictionary<string, Sensor> _inner = new();

        public int Count => _inner.Count;

        public Sensor? this[string mac]
            => _inner.TryGetValue(mac, out var s) ? s : null;

        public bool Add(Sensor sensor) => _inner.TryAdd(sensor.MacAddress, sensor);
        public bool Remove(string mac) => _inner.TryRemove(mac, out _);
        public bool Contains(string mac) => _inner.ContainsKey(mac);

        public IEnumerator<Sensor> GetEnumerator() => _inner.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Troelsen, A. and Japikse, P., 2017. Pro C# 7: With .NET and 
       .NET Core. 8th ed. New York: Apress.
   ============================================================ */