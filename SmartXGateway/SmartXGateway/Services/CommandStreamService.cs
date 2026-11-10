using SmartXGateway.Models;

namespace SmartXGateway.Services
{
   
    public class CommandStreamService
    {
        public const double PowerSpikeThreshold = 1500.0;
        public const double NegativeValueThreshold = 0.0;
        public const int ExplicitPriorityCutoff = 5;
        private const int PriorityBurstPerCycle = 3;

        private readonly TelemetryQueueService _standardQueue;
        private readonly CriticalAlertPriorityQueueService _priorityQueue;
        private readonly CommandHistoryStackService _commandStack;
        private int _processedCount;
        private readonly object _processedLock = new();

        public CommandStreamService()
        {
            _standardQueue = new TelemetryQueueService();
            _priorityQueue = new CriticalAlertPriorityQueueService();
            _commandStack = new CommandHistoryStackService();
        }

        // Expose the individual structures clearly for demonstration/testing.
        public TelemetryQueueService StandardQueue => _standardQueue;
        public CriticalAlertPriorityQueueService PriorityQueue => _priorityQueue;
        public CommandHistoryStackService CommandStack => _commandStack;

        public int QueuedCount => _standardQueue.Count;
        public int PriorityCount => _priorityQueue.Count;
        public int ProcessedCount
        {
            get { lock (_processedLock) return _processedCount; }
        }

        public int UndoableCommandCount => _commandStack.Count;
        public IReadOnlyList<CommandRecord> CommandHistory => _commandStack.History;

        public bool Enqueue(TelemetryPacket<double> packet)
        {
            bool critical = IsCritical(packet);

            if (critical)
            {
                _priorityQueue.Enqueue(packet, CalculatePriority(packet));
            }
            else
            {
                _standardQueue.Enqueue(packet);
            }

            return critical;
        }

        public List<(TelemetryPacket<double> Packet, bool WasPriority)> DequeueBatch(int max = 20)
        {
            if (max <= 0)
                return new List<(TelemetryPacket<double>, bool)>();

            var drained = new List<(TelemetryPacket<double>, bool)>();

            while (drained.Count < max && (_priorityQueue.Count > 0 || _standardQueue.Count > 0))
            {
                int priorityTaken = 0;

                // Critical telemetry gets processed before standard FIFO data.
                while (priorityTaken < PriorityBurstPerCycle &&
                       drained.Count < max &&
                       _priorityQueue.TryDequeue(out var priorityPacket))
                {
                    drained.Add((priorityPacket!, true));
                    priorityTaken++;
                }

                // After critical packets, process the next standard packet FIFO.
                if (drained.Count < max && _standardQueue.TryDequeue(out var standardPacket))
                {
                    drained.Add((standardPacket!, false));
                }
            }

            lock (_processedLock)
            {
                _processedCount += drained.Count;
            }

            return drained;
        }

        public CommandRecord PushCommand(string mac, string type, string payload, string issuedBy)
            => _commandStack.Push(mac, type, payload, issuedBy);

        public CommandRecord? UndoLast()
            => _commandStack.UndoLast();

        public CommandRecord? PeekLast()
            => _commandStack.Peek();

        private static bool IsCritical(TelemetryPacket<double> packet)
        {
            return packet.Priority >= ExplicitPriorityCutoff ||
                   packet.Value > PowerSpikeThreshold ||
                   packet.Value < NegativeValueThreshold;
        }

        private static int CalculatePriority(TelemetryPacket<double> packet)
        {
            if (packet.Priority >= ExplicitPriorityCutoff)
                return -1000 - packet.Priority;

            if (packet.Value > PowerSpikeThreshold)
                return -1000;

            // Negative values represent a severe/invalid drop and must bypass FIFO.
            return -999;
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