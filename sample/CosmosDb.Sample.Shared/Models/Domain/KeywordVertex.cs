using CosmosDB.Net.Domain.Attributes;

namespace CosmosDb.Sample.Shared.Models.Domain
{
    [Label(Value = "Keyword")]
    public class KeywordVertex
    {
        [Id]
        public string Keyword { get; set; }

        [PartitionKey]
        public string Pk { get; }  = "Keyword";
    }
}
