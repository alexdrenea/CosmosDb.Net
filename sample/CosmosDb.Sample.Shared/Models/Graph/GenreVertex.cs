using CosmosDb.Attributes;

namespace CosmosDb.Sample.Shared.Models.Graph
{
    public class GenreVertex
    {
        [Id]
        public string Genre { get; set; }

        [PartitionKey]
        public string Pk => "Genre";
    }
}
