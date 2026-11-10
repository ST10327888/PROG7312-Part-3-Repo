using SmartXGateway.Models;

namespace SmartXGateway.Services
{
   
    public class CriticalAlertPriorityQueueService
    {
        private readonly PriorityQueue<TelemetryPacket<double>, int> _queue = new();
        private readonly object _lock = new();

        public int Count
        {
            get { lock (_lock) return _queue.Count; }
        }

        public void Enqueue(TelemetryPacket<double> packet, int priority)
        {
            lock (_lock)
            {
                _queue.Enqueue(packet, priority);
            }
        }

        public bool TryDequeue(out TelemetryPacket<double>? packet)
        {
            lock (_lock)
            {
                if (_queue.TryDequeue(out var result, out _))
                {
                    packet = result;
                    return true;
                }

                packet = null;
                return false;
            }
        }

        public void Clear()
        {
            lock (_lock) _queue.Clear();
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