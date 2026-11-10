using SmartXGateway.Models;

namespace SmartXGateway.Services
{


    public class CommandHistoryStackService
    {
        private readonly Stack<CommandRecord> _stack = new();
        private readonly List<CommandRecord> _history = new();
        private readonly object _lock = new();
        private int _sequenceCounter;

        public int Count
        {
            get { lock (_lock) return _stack.Count; }
        }

        public IReadOnlyList<CommandRecord> History
        {
            get
            {
                lock (_lock)
                {
                    return _history
                        .OrderByDescending(c => c.SequenceNumber)
                        .ToList();
                }
            }
        }

        public CommandRecord Push(string mac, string type, string payload, string issuedBy)
        {
            lock (_lock)
            {
                var command = new CommandRecord
                {
                    DeviceMac = mac,
                    CommandType = type,
                    Payload = payload,
                    IssuedBy = issuedBy,
                    SequenceNumber = ++_sequenceCounter,
                    InverseCommand = InverseOf(type)
                };

                _stack.Push(command);
                _history.Add(command);

                if (_history.Count > 200)
                    _history.RemoveAt(0);

                return command;
            }
        }

        public CommandRecord? UndoLast()
        {
            lock (_lock)
            {
                while (_stack.Count > 0)
                {
                    var command = _stack.Pop();

                    if (!command.IsUndone)
                    {
                        command.IsUndone = true;
                        return command;
                    }
                }

                return null;
            }
        }

        public CommandRecord? Peek()
        {
            lock (_lock)
            {
                return _stack.Count > 0 ? _stack.Peek() : null;
            }
        }

        private static string InverseOf(string command) => command switch
        {
            "OPEN_VALVE" => "CLOSE_VALVE",
            "CLOSE_VALVE" => "OPEN_VALVE",
            "SHUTDOWN_RELAY" => "RESTORE_RELAY",
            "RESTORE_RELAY" => "SHUTDOWN_RELAY",
            "TOGGLE_RELAY" => "TOGGLE_RELAY",
            "REBOOT_NODE" => "NO_OP",
            _ => "NO_OP"
        };
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