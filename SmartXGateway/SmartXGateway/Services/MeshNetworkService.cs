using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartXGateway.Services
{
    public class MeshEdge
    {
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int Weight { get; set; }
    }

    public class MeshNetworkService
    {
        private readonly Dictionary<string, List<MeshEdge>> _graph = new();

        public void AddNode(string mac)
        {
            if (!_graph.ContainsKey(mac)) _graph[mac] = new List<MeshEdge>();
        }

        public void AddEdge(string source, string destination, int weight)
        {
            AddNode(source);
            AddNode(destination);

            _graph[source].Add(new MeshEdge { Source = source, Destination = destination, Weight = weight });
            _graph[destination].Add(new MeshEdge { Source = destination, Destination = source, Weight = weight });
        }

        public List<string> FindShortestPathBFS(string start, string end)
        {
            if (!_graph.ContainsKey(start) || !_graph.ContainsKey(end)) return new List<string>();

            var queue = new Queue<List<string>>();
            var visited = new HashSet<string>();

            queue.Enqueue(new List<string> { start });
            visited.Add(start);

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                var current = path.Last();

                if (current == end) return path;

                foreach (var edge in _graph[current])
                {
                    if (!visited.Contains(edge.Destination))
                    {
                        visited.Add(edge.Destination);
                        var newPath = new List<string>(path) { edge.Destination };
                        queue.Enqueue(newPath);
                    }
                }
            }
            return new List<string>();
        }

        public List<string> FindAllConnectedDFS(string start)
        {
            var visited = new HashSet<string>();
            var result = new List<string>();
            if (!_graph.ContainsKey(start)) return result;

            DfsRec(start, visited, result);
            return result;
        }

        private void DfsRec(string current, HashSet<string> visited, List<string> result)
        {
            visited.Add(current);
            result.Add(current);

            foreach (var edge in _graph[current])
            {
                if (!visited.Contains(edge.Destination))
                {
                    DfsRec(edge.Destination, visited, result);
                }
            }
        }

        public List<MeshEdge> CalculateMST()
        {
            var mst = new List<MeshEdge>();
            if (_graph.Count == 0) return mst;

            var visited = new HashSet<string>();
            var priorityQueue = new PriorityQueue<MeshEdge, int>();

            var startNode = _graph.Keys.First();
            visited.Add(startNode);

            foreach (var edge in _graph[startNode])
                priorityQueue.Enqueue(edge, edge.Weight);

            while (priorityQueue.Count > 0 && visited.Count < _graph.Count)
            {
                var edge = priorityQueue.Dequeue();

                if (!visited.Contains(edge.Destination))
                {
                    visited.Add(edge.Destination);
                    mst.Add(edge);

                    foreach (var nextEdge in _graph[edge.Destination])
                    {
                        if (!visited.Contains(nextEdge.Destination))
                            priorityQueue.Enqueue(nextEdge, nextEdge.Weight);
                    }
                }
            }

            return mst;
        }

        public Dictionary<string, List<MeshEdge>> GetNetworkMap() => _graph;
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