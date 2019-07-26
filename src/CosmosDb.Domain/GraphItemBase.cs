using System.Collections.Generic;

namespace CosmosDb.Domain
{
    public class GraphItemBase
    {
        public GraphItemBase() { }

        public GraphItemBase(string id, string partitionKey, string label)
        {
            Id = id;
            PartitionKey = partitionKey;
            Label = label;
        }

        public string Id { get; set; }
        public string PartitionKey { get; set; }
        public string Label { get; set; }
    }

    public class EdgeDefinition
    {
        public EdgeDefinition() { }

        public EdgeDefinition(object edge, GraphItemBase sourceVetex, GraphItemBase targetVertex, bool single = false)
        {
            EdgeEntity = edge;
            SourceVertex = sourceVetex;
            TargetVertex = targetVertex;
            Single = single;
        }

        public object EdgeEntity { get; set; }
        public GraphItemBase SourceVertex { get; set; }
        public GraphItemBase TargetVertex { get; set; }

        public bool Single { get; set; }
    }
}
