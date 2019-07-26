using CosmosDb.Attributes;

namespace CosmosDb.Sample.Shared.Models.Graph
{
    /// <summary>
    /// Represents an Actor
    /// </summary>
    public class ActorVertex
    {
        [PartitionKey]
        public string Name { get; set; }
    }
}
