using SmartXGateway.Models;

namespace SmartXGateway.Services
{
    public class DeploymentValidator
    {

        public string? Validate(DeploymentNode node, int depth = 0)
        {
            // BASE CASE — leaf node must be configured
            if (node.Children == null || node.Children.Count == 0)
            {
                return node.IsConfigured
                    ? null
                    : $"Unconfigured leaf node '{node.Name}' at depth {depth}.";
            }

            // RECURSIVE CASE
            if (!node.IsConfigured)
                return $"Unconfigured node '{node.Name}' ({node.NodeType}) at depth {depth}.";

            foreach (var child in node.Children)
            {
                var err = Validate(child, depth + 1);
                if (err != null) return err;
            }
            return null;
        }

        public DeploymentNode BuildSampleTree() => new DeploymentNode
        {
            Name = "Facility A",
            NodeType = "Facility",
            Children = new()
            {
                new DeploymentNode
                {
                    Name = "Zone 1", NodeType = "Zone",
                    Children = new()
                    {
                        new DeploymentNode
                        {
                            Name = "Sub-Zone B", NodeType = "Sub-Zone",
                            Children = new()
                            {
                                new DeploymentNode { Name = "Node-001", NodeType = "Node" },
                                new DeploymentNode { Name = "Node-002", NodeType = "Node" },
                            }
                        }
                    }
                },
                new DeploymentNode
                {
                    Name = "Zone 2", NodeType = "Zone",
                    Children = new()
                    {
                        new DeploymentNode { Name = "Sub-Zone C", NodeType = "Sub-Zone", IsConfigured = true }
                    }
                }
            }
        };
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Wentworth, P., 2015. Think Sharply with C#. [Online] Available 
       at: http://www.ict.ru.ac.za/Resources/ThinkSharply/ThinkSharply/index.html [Accessed 18 November 2025].
   ============================================================ */