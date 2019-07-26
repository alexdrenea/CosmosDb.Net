namespace CosmosDb.Domain
{
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
