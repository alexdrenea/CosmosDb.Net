using CosmosDb.Attributes;

namespace CosmosDb.Tests.TestData.Models
{
    public class Cast
    {
        [Id]
        public string Id => $"{MovieTitle}-{Order}";

        [PartitionKey]
        public string MovieTitle { get; set; }
        public string Name { get; set; }
        public string Character { get; set; }
        public int Order { get; set; }
        public bool Uncredited { get; set; }

        public static Cast GetTestMovieCast(string title)
        {
            return new Cast
            {
                MovieTitle = title,
                Order = 0,
                Uncredited = false,
                Character = $"{title}-character",
                Name = $"{title}-name",
            };
        }
    }

    public class MovieCastEdge
    {
        public int Order { get; set; }
    }
}
