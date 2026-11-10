namespace SmartXGateway.Models
{
    public class TimelineBucket
    {
        public DateTime BucketStart { get; set; }
        public int PacketCount { get; set; }
        public int AnomalyCount { get; set; }
        public double AverageValue { get; set; }
        public double MaxValue { get; set; }
        public double MinValue { get; set; }
        private double _sum;

        public void Add(double value, bool isAnomaly)
        {
            PacketCount++;
            _sum += value;
            AverageValue = _sum / PacketCount;
            if (PacketCount == 1) { MaxValue = MinValue = value; }
            else
            {
                if (value > MaxValue) MaxValue = value;
                if (value < MinValue) MinValue = value;
            }
            if (isAnomaly) AnomalyCount++;
        }
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