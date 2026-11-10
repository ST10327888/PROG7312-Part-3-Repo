namespace SmartXGateway.Models
{
    public class DeviceState
    {
        public string MacAddress { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public SensorCategory Category { get; set; }
        public double LastValue { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public bool IsConnected { get; set; } = true;
        public bool IsAlerted { get; set; } = false;
        public string? CurrentAlert { get; set; }
        public int PacketCount { get; set; }

        
        public Dictionary<string, string> DeviceFlags { get; set; } = new();
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Troelsen, A and Japikse, P. 2017. Pro C# 7. With .NET and
       .NET Core. 8th ed. Apress.

   Peter Wentworth, 2015. Think Sharply with C#. [Online]
       Available at:
       http://www.ict.ru.ac.za/Resources/ThinkSharply/ThinkSharply/index.html
       [Accessed on 18 November 2025].
   ============================================================ */