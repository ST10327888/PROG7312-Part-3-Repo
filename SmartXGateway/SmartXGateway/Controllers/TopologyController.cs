using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartXGateway.Data;
using SmartXGateway.Services;

namespace SmartXGateway.Controllers
{
    [Authorize]
    public class TopologyController : Controller
    {
        private readonly SensorStore _store;

        public TopologyController(SensorStore store)
        {
            _store = store;
        }

        public IActionResult Index()
        {
            var sensors = _store.AllSensors.ToList();

            // Dynamically build the Graph based on registered sensors
            if (sensors.Count >= 2)
            {
                var newNetwork = new MeshNetworkService();
                foreach (var s in sensors) newNetwork.AddNode(s.MacAddress);

                var random = new Random();
                for (int i = 0; i < sensors.Count; i++)
                {
                    for (int j = i + 1; j < sensors.Count; j++)
                    {
                        // Mesh topology logic
                        if (random.NextDouble() > 0.6 || j == i + 1)
                        {
                            newNetwork.AddEdge(sensors[i].MacAddress, sensors[j].MacAddress, random.Next(5, 50));
                        }
                    }
                }
                _store.MeshNetwork = newNetwork;
            }

            ViewBag.Sensors = sensors;
            return View();
        }

        [HttpGet]
        public IActionResult GetMST()
        {
            var mst = _store.MeshNetwork.CalculateMST();
            return Ok(mst);
        }

        [HttpGet]
        public IActionResult FindPath(string start, string end)
        {
            var path = _store.MeshNetwork.FindShortestPathBFS(start, end);
            return Ok(path);
        }

        [HttpGet]
        public IActionResult QueryHistory(string mac, System.DateTime from, System.DateTime to)
        {
            var results = _store.GetTelemetryRange(mac, from, to);
            return Ok(results.Select(r => new { timestamp = r.Timestamp, value = r.Value }));
        }
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Troelsen, A. and Japikse, P., 2017. Pro C# 7: With .NET and 
       .NET Core. 8th ed. New York: Apress.
       
   Wentworth, P., 2015. Think Sharply with C#. [Online] Available 
       at: http://www.ict.ru.ac.za/Resources/ThinkSharply/ThinkSharply/index.html [Accessed 18 November 2025].
   ============================================================ */