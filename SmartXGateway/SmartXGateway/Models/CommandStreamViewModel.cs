namespace SmartXGateway.Models
{
    public class CommandStreamViewModel
    {
        public List<DeviceState> Devices { get; set; } = new();
        public List<CommandRecord> CommandHistory { get; set; } = new();
        public List<SuggestedAction> Suggestions { get; set; } = new();
        public List<TimelineBucket> Timeline { get; set; } = new();
        public List<string> ActiveAlerts { get; set; } = new();
        public int QueuedCount { get; set; }
        public int PriorityCount { get; set; }
        public int TotalPackets { get; set; }
        public int AnomalyCount { get; set; }
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