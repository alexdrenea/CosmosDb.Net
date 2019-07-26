using CosmosDb.Attributes;

namespace CosmosDb.Sample.Shared.Models.Graph
{
    public class MovieGenreEdge {
        [Label]
        public string Label = "isGenre";
    }
}
