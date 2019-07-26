using CosmosDb.Attributes;

namespace CosmosDb.Sample.Shared.Models.Graph
{
    public class MovieCastEdge
    {
        public int Order { get; set; }

        [Label]
        public string Label = "hasCast";
    }
}
