using CosmosDB.Net.Domain.Attributes;

namespace CosmosDb.Sample.Shared.Models.Domain
{
    [Label(Value = "Genre")]
    public class GenreVertex
    {
        [Id]
        public string Genre { get; set; }

        [PartitionKey]
        public string Pk { get; } = "Genre";
    }
}
 