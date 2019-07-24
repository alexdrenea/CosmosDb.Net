using CosmosDb.Attributes;
using System;

namespace CosmosDb.Tests.TestData.Models
{
    public class Cast
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

        public static Cast GetTestMovieCast(string title)
        {
            return new Cast
            {
                MovieId = (new Random()).Next(1000).ToString(),
                MovieTitle = title,
                Order = 0,
                Uncredited = false,
                Character = $"{title}-character",
                ActorName = $"{title}-name",
            };
        }

        public static Cast GetCastFromCsv(CastCsv castCsv, string movieTitle)
        {
            return new Cast
            {
                MovieTitle = movieTitle,
                MovieId = castCsv.TmdbId,
                ActorName = castCsv.Name,
                Character = castCsv.Character,
                Order = castCsv.Order,
                Uncredited = castCsv.Uncredited,
            };
        }
    }

    public class MovieCastEdge
    {
        public int Order { get; set; }
    }
}
