using System.Collections.Generic;

namespace SmartXGateway.Models
{
    public class DeploymentNode
    {
        public string Name { get; set; } = string.Empty;
        public string NodeType { get; set; } = "Zone"; // Facility, Zone, Sub-Zone, Node
        public bool IsConfigured { get; set; } = true;
        public List<DeploymentNode> Children { get; set; } = new();
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