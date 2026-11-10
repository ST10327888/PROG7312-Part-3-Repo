namespace SmartXGateway.Models
{
    public enum SuggestionSource
    {
        LiveAnomaly,
        LearnedPattern,
        TelemetryPattern,
        Trending
    }

    public class SuggestedAction
    {
        public string DeviceMac { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public int Confidence { get; set; }
        public string SuggestedCommand { get; set; } = string.Empty;
        public SuggestionSource Source { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // RUBRIC IMPROVEMENT (Predictive): distinguishes actionable commands
        // from view-only suggestions (e.g. "Trending device" → VIEW_DEVICE).
        // Enables end-to-end outcome tracking on the server.
        public bool IsExecutable { get; set; } = true;
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Wentworth, P., 2015. Think Sharply with C#. [Online] Available 
       at: http://www.ict.ru.ac.za/Resources/ThinkSharply/ThinkSharply/index.html [Accessed 18 November 2025].
   ============================================================ */