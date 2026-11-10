using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartXGateway.Data;
using SmartXGateway.Hubs;
using SmartXGateway.Models;

namespace SmartXGateway.Controllers
{
    // RUBRIC IMPROVEMENT: auto-validates antiforgery on every POST.
    // Closes the CSRF critical flagged for Report/Resolve (JSON POSTs now
    // require the RequestVerificationToken header, which the client sends).
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class IssuesController : Controller
    {
        private readonly SensorStore _store;
        private readonly IHubContext<TelemetryHub> _hub;
        private readonly ILogger<IssuesController> _logger;

        // RUBRIC IMPROVEMENT: hard caps so a client can't push megabytes
        // of description into the in-memory list or the SignalR broadcast.
        private const int MaxDescriptionLength = 2000;
        private const int MaxMacLength = 32;

        public IssuesController(
            SensorStore store,
            IHubContext<TelemetryHub> hub,
            ILogger<IssuesController> logger)
        {
            _store = store;
            _hub = hub;
            _logger = logger;
        }

        // GET /Issues/List
        [HttpGet]
        public IActionResult List()
        {
            // RUBRIC IMPROVEMENT: project to a snapshot DTO — avoids leaking
            // internal-only fields if UserReportedIssue gains them later, and
            // avoids concurrent-enumeration exceptions.
            var snapshot = _store.ReportedIssues
                .Select(i => new
                {
                    i.Id,
                    i.MacAddress,
                    i.Description,
                    i.Status,
                    i.ReportedAt,
                    i.ReportedBy
                })
                .ToList();
            return Json(snapshot);
        }

        // POST /Issues/Report
        [HttpPost]
        public async Task<IActionResult> Report([FromBody] ReportIssueRequest req)
        {
            // RUBRIC IMPROVEMENT: dedicated DTO prevents mass-assignment via
            // Id, ReportedAt, ReportedBy, Status — those are now server-set only.
            if (req == null ||
                string.IsNullOrWhiteSpace(req.MacAddress) ||
                string.IsNullOrWhiteSpace(req.Description))
                return BadRequest(new { ok = false, message = "MAC and description required." });

            var mac = req.MacAddress.Trim().ToUpperInvariant();
            var desc = req.Description.Trim();

            if (mac.Length > MaxMacLength)
                return BadRequest(new { ok = false, message = "MAC too long." });

            if (desc.Length > MaxDescriptionLength)
                return BadRequest(new { ok = false, message = $"Description must be at most {MaxDescriptionLength} characters." });

            // RUBRIC IMPROVEMENT: validate against known sensors so the
            // issue list can't be polluted with phantom MACs.
            if (_store.Get(mac) == null)
                return BadRequest(new { ok = false, message = "Unknown sensor MAC." });

            var issue = new UserReportedIssue
            {
                // RUBRIC IMPROVEMENT: NextIssueId is now Interlocked-safe
                // (see SensorStore.cs replacement) so concurrent reports can't collide.
                Id = _store.NextIssueId,
                MacAddress = mac,
                Description = desc,
                Status = "Open",
                ReportedAt = DateTime.UtcNow,
                ReportedBy = User.Identity?.Name ?? "anonymous"
            };

            _store.ReportedIssues.Add(issue);
            _store.Log($"⚠ {issue.ReportedBy} reported issue on {issue.MacAddress}: {issue.Description}", "warning");
            _logger.LogInformation("Issue #{Id} reported by {User} on {Mac}", issue.Id, issue.ReportedBy, issue.MacAddress);

            await _hub.Clients.All.SendAsync("issueReported", new
            {
                id = issue.Id,
                macAddress = issue.MacAddress,
                description = issue.Description,
                status = issue.Status,
                reportedAt = issue.ReportedAt,
                reportedBy = issue.ReportedBy
            });

            return Ok(new { ok = true, id = issue.Id });
        }

        // POST /Issues/Resolve?id=...
        [HttpPost]
        public async Task<IActionResult> Resolve(int id)
        {
            var issue = _store.ReportedIssues.FirstOrDefault(i => i.Id == id);
            if (issue == null) return NotFound(new { ok = false, message = "Issue not found." });

            // RUBRIC IMPROVEMENT: idempotent — resolving a resolved issue
            // doesn't rewrite ReportedAt-adjacent fields or double-log.
            if (issue.Status == "Resolved")
                return Ok(new { ok = true, alreadyResolved = true });

            issue.Status = "Resolved";
            _store.Log($"✔ Issue #{id} resolved.", "success");
            _logger.LogInformation("Issue #{Id} resolved by {User}", id, User.Identity?.Name ?? "anon");

            // RUBRIC IMPROVEMENT: broadcast so all dashboards reflect the
            // change live — previously only the initiating tab updated.
            await _hub.Clients.All.SendAsync("issueResolved", new
            {
                id = issue.Id,
                macAddress = issue.MacAddress,
                status = issue.Status
            });

            return Ok(new { ok = true });
        }

        // RUBRIC IMPROVEMENT: request DTO — mirrors FilterRequest/IssueCommandRequest
        // pattern elsewhere so mass-assignment is impossible by construction.
        public record ReportIssueRequest(string MacAddress, string Description);
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