namespace SmartXGateway.Models
{

    public class CommandRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string DeviceMac { get; set; } = string.Empty;
        public string CommandType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public string IssuedBy { get; set; } = "system";
        public bool IsUndone { get; set; } = false;
        public int SequenceNumber { get; set; }

      
        public string InverseCommand { get; set; } = string.Empty;
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