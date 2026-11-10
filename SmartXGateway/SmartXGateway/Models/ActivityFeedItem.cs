namespace SmartXGateway.Models
{
    public class ActivityFeedItem
    {
        public string Message { get; set; } = string.Empty;
        public string Level { get; set; } = "info"; // info, success, warning, danger
        // RUBRIC IMPROVEMENT: UtcNow for consistency with all other models
        // (CommandRecord, DeviceState, Sensor, SuggestedAction all use UtcNow).
       
        public System.DateTime Timestamp { get; set; } = System.DateTime.UtcNow;
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