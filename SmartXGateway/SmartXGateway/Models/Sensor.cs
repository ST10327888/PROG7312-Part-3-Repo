using System;

namespace SmartXGateway.Models
{
    public enum SensorCategory
    {
        Environmental = 0,
        PowerConsumption = 1,
        Actuator = 2
    }

    public class Sensor
    {
        public int Id { get; set; }
        public string MacAddress { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public SensorCategory Category { get; set; }
        public double LastValue { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public string? AttachmentPath { get; set; }
        public bool IsConnected { get; set; } = true;

       
        public static Sensor operator +(Sensor a, Sensor b)
        {
            return new Sensor
            {
                MacAddress = $"AGG[{a.MacAddress}+{b.MacAddress}]",
                Location = $"Aggregate of {a.Location} & {b.Location}",
                Category = a.Category,
                LastValue = a.LastValue + b.LastValue
            };
        }

        // Delta / consumption difference
        public static Sensor operator -(Sensor a, Sensor b)
        {
            return new Sensor
            {
                MacAddress = $"DELTA[{a.MacAddress}-{b.MacAddress}]",
                Location = $"Delta between {a.Location} & {b.Location}",
                Category = a.Category,
                LastValue = a.LastValue - b.LastValue
            };
        }

        // Comparison operators work on LastValue
        public static bool operator >(Sensor a, Sensor b) => a.LastValue > b.LastValue;
        public static bool operator <(Sensor a, Sensor b) => a.LastValue < b.LastValue;
        public static bool operator >=(Sensor a, Sensor b) => a.LastValue >= b.LastValue;
        public static bool operator <=(Sensor a, Sensor b) => a.LastValue <= b.LastValue;

        public static bool operator ==(Sensor? a, Sensor? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.MacAddress == b.MacAddress;
        }
        public static bool operator !=(Sensor? a, Sensor? b) => !(a == b);

        public override bool Equals(object? obj) => obj is Sensor s && s.MacAddress == MacAddress;
        public override int GetHashCode() => MacAddress.GetHashCode();
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Troelsen, A. and Japikse, P., 2017. Pro C# 7: With .NET and 
       .NET Core. 8th ed. New York: Apress.
   ============================================================ */