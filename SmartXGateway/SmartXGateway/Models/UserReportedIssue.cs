using System;

namespace SmartXGateway.Models
{
    
    public class UserReportedIssue
    {
        public int Id { get; set; }
        public string MacAddress { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open"; // Open, Investigating, Resolved
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
        public string ReportedBy { get; set; } = "system";
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