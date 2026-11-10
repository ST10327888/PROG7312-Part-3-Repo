using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartXGateway.Hubs
{
    // RUBRIC IMPROVEMENT: the hub now requires authentication.
    // Anonymous clients can no longer receive broadcast telemetry / issues / alerts.
    [Authorize]
    public class TelemetryHub : Hub
    {
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Troelsen, A. and Japikse, P., 2017. Pro C# 7: With .NET and 
       .NET Core. 8th ed. New York: Apress.
       
   Wentworth, P., 2015. Think Sharply with C#. [Online] Available 
       at: http://www.ict.ru.ac.za/Resources/ThinkSharply/ThinkSharply/index.html [Accessed 18 November 2025].

  Microsoft, 2024. ASP.NET Core SignalR documentation. [online] 
       Available at: <https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction> 
       [Accessed 18 November 2025].
   ============================================================ */