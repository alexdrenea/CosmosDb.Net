using CosmosDB.Net.Domain.Attributes;
using System;

namespace CosmosDb.Tests.TestData.Models
{
    public class Rating
    {
      
        [PartitionKey]
        public string MovieTitle { get; set; }
        public string SiteName { get; set; }
        public decimal AvgRating { get; set; }
        public int MaxRating { get; set; }
        public int Votes { get; set; }

        public static Rating GetTestRating(string title, int maxRating = 5)
        {
            var rnd = new Random();
            return new Rating
            {
                MovieTitle = title,
                MaxRating = maxRating,
                AvgRating = (decimal)(rnd.Next() * maxRating),
                Votes = rnd.Next(1000),
                SiteName = "Test Site"
            };
        }
    }

    public class MovieRatingEdge
    {
        public string SiteName { get; set; }
    }
}
