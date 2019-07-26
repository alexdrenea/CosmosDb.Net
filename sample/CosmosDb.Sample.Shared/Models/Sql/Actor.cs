using CosmosDb.Attributes;
using CosmosDb.Sample.Shared.Models.Csv;

namespace CosmosDb.Sample.Shared.Models.Sql
{
    public class Actor
    {
        [Id]
        public string Id => $"{MovieTitle}-{Order}";

        [PartitionKey]
        public string ActorName { get; set; }

        public string MovieTitle { get; set; }
        public string MovieId { get; set; }

        public string Character { get; set; }
        public int Order { get; set; }
        public bool Uncredited { get; set; }
    }
}
