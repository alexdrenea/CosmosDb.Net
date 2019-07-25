using CosmosDb.Attributes;

namespace CosmosDb.Sample.Shared.Models.Graph
{
    public class KeywordVertex
    {
        [Id]
        public string Keyword { get; set; }

        [PartitionKey]
        public string Pk => "Keyword";
    }
}
