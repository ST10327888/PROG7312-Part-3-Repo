using System;
using System.Collections.Generic;

namespace SmartXGateway.Models
{
    public class AvlNode
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public int Height { get; set; }
        public AvlNode? Left { get; set; }
        public AvlNode? Right { get; set; }

        public AvlNode(DateTime timestamp, double value)
        {
            Timestamp = timestamp;
            Value = value;
            Height = 1;
        }
    }

    public class AvlTree
    {
        private AvlNode? _root;

        public void Insert(DateTime timestamp, double value)
        {
            _root = InsertRec(_root, timestamp, value);
        }

        public List<(DateTime Timestamp, double Value)> GetRange(DateTime start, DateTime end)
        {
            var results = new List<(DateTime, double)>();
            GetRangeRec(_root, start, end, results);
            return results;
        }

        private AvlNode InsertRec(AvlNode? node, DateTime timestamp, double value)
        {
            if (node == null) return new AvlNode(timestamp, value);

            if (timestamp < node.Timestamp)
                node.Left = InsertRec(node.Left, timestamp, value);
            else if (timestamp > node.Timestamp)
                node.Right = InsertRec(node.Right, timestamp, value);
            else
            {
                node.Value = value;
                return node;
            }

            node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
            int balance = GetBalance(node);

            if (balance > 1 && timestamp < node.Left!.Timestamp) return RightRotate(node);
            if (balance < -1 && timestamp > node.Right!.Timestamp) return LeftRotate(node);
            if (balance > 1 && timestamp > node.Left!.Timestamp)
            {
                node.Left = LeftRotate(node.Left);
                return RightRotate(node);
            }
            if (balance < -1 && timestamp < node.Right!.Timestamp)
            {
                node.Right = RightRotate(node.Right);
                return LeftRotate(node);
            }

            return node;
        }

        private void GetRangeRec(AvlNode? node, DateTime start, DateTime end, List<(DateTime, double)> results)
        {
            if (node == null) return;

            if (node.Timestamp > start)
                GetRangeRec(node.Left, start, end, results);

            if (node.Timestamp >= start && node.Timestamp <= end)
                results.Add((node.Timestamp, node.Value));

            if (node.Timestamp < end)
                GetRangeRec(node.Right, start, end, results);
        }

        private int Height(AvlNode? node) => node == null ? 0 : node.Height;
        private int GetBalance(AvlNode? node) => node == null ? 0 : Height(node.Left) - Height(node.Right);

        private AvlNode RightRotate(AvlNode y)
        {
            AvlNode x = y.Left!;
            AvlNode? T2 = x.Right;
            x.Right = y;
            y.Left = T2;
            y.Height = Math.Max(Height(y.Left), Height(y.Right)) + 1;
            x.Height = Math.Max(Height(x.Left), Height(x.Right)) + 1;
            return x;
        }

        private AvlNode LeftRotate(AvlNode x)
        {
            AvlNode y = x.Right!;
            AvlNode? T2 = y.Left;
            y.Left = x;
            x.Right = T2;
            x.Height = Math.Max(Height(x.Left), Height(x.Right)) + 1;
            y.Height = Math.Max(Height(y.Left), Height(y.Right)) + 1;
            return y;
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