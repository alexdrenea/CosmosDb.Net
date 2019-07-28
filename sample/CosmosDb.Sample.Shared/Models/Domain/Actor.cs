using CosmosDB.Net.Domain.Attributes;

namespace CosmosDb.Sample.Shared.Models.Domain
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
