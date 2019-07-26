using CosmosDb.Attributes;

namespace CosmosDb.Sample.Shared.Models.Graph
{
    public class MovieKeywordEdge {
        [Label]
        public string Label = "hasKeyword";
    }
}
