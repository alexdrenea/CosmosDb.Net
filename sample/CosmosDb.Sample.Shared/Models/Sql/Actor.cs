using CosmosDb.Attributes;

namespace CosmosDb.Sample.Shared.Models.Sql
{
    /// <summary>
    /// Represents an Actor
    /// </summary>
    public class Actor
    {
        [PartitionKey]
        public string Name { get; set; }
    }
}
