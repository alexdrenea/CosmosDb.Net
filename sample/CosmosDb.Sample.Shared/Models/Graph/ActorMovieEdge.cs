using CosmosDb.Attributes;

namespace CosmosDb.Sample.Shared.Models.Graph
{
    public class ActorMovieEdge
    {
        public int Order { get; set; }

        [Label]
        public string Label = "playedIn";
    }
}
