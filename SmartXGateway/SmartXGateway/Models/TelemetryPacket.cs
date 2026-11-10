using System;

namespace SmartXGateway.Models
{
    
    public class TelemetryPacket<T>
    {
        public string DeviceMac { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public T Value { get; set; } = default!;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int Priority { get; set; } = 0;

        public TelemetryPacket() { }

        public TelemetryPacket(string mac, string metric, T value, int priority = 0)
        {
            DeviceMac = mac;
            MetricName = metric;
            Value = value;
            Priority = priority;
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
            => $"[{Timestamp:HH:mm:ss}] {DeviceMac} | {MetricName} = {Value} (P{Priority})";
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Troelsen, A. and Japikse, P., 2017. Pro C# 7: With .NET and 
       .NET Core. 8th ed. New York: Apress.
   ============================================================ */