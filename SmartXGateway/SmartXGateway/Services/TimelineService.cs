using SmartXGateway.Models;

namespace SmartXGateway.Services
{
  
    public class TimelineService
    {
        private readonly SortedDictionary<DateTime, TimelineBucket> _timeline = new();
        private readonly object _lock = new();
        private const int BucketMinutes = 5;
        private const int MaxBuckets = 500;

        public int BucketCount
        {
            get { lock (_lock) return _timeline.Count; }
        }

        public void Record(TelemetryPacket<double> packet, bool isAnomaly)
        {
            var bucketKey = RoundDown(packet.Timestamp);

            lock (_lock)
            {
                if (!_timeline.TryGetValue(bucketKey, out var bucket))
                {
                    bucket = new TimelineBucket { BucketStart = bucketKey };
                    _timeline.Add(bucketKey, bucket);

                    if (_timeline.Count > MaxBuckets)
                        _timeline.Remove(_timeline.Keys.First());
                }

                bucket.Add(packet.Value, isAnomaly);
            }
        }

        public IReadOnlyList<TimelineBucket> GetRange(DateTime from, DateTime to)
        {
            if (from > to)
                (from, to) = (to, from);

            lock (_lock)
            {
                return _timeline
                    .Where(kv => kv.Key >= from && kv.Key <= to)
                    .Select(kv => kv.Value)
                    .ToList();
            }
        }

        public IReadOnlyList<TimelineBucket> GetRecent(int count = 40)
        {
            if (count <= 0)
                return Array.Empty<TimelineBucket>();

            lock (_lock)
            {
                return _timeline.Values
                    .Reverse()
                    .Take(count)
                    .Reverse()
                    .ToList();
            }
        }

        private static DateTime RoundDown(DateTime timestamp)
        {
            var utc = timestamp.Kind == DateTimeKind.Utc
                ? timestamp
                : timestamp.ToUniversalTime();

            return new DateTime(
                utc.Year,
                utc.Month,
                utc.Day,
                utc.Hour,
                utc.Minute - utc.Minute % BucketMinutes,
                0,
                DateTimeKind.Utc);
        }
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Jamro, M., 2018. C# Data Structures and Algorithms. Birmingham: 
       Packt Publishing.
   
   Nakov, S. and Kolev, V., 2013. Fundamentals of Computer 
       Programming with C#. Sofia: Svetlin Nakov & Co. Available at: 
       https://www.free-ebooks.net/computer-internet/Fundamentals-of-Computer-Programming-with-C/pdf [Accessed 18 November 2025].
   ============================================================ */