using CosmosDB.Net.Domain.Attributes;
using System;
using System.Collections.Generic;

namespace CosmosDb.Sample.Shared.Models.Domain
{
    public enum MovieFormat
    {
        Regular,
        Imax,
        _3D
    }
   
    /// <summary>
    /// A more detailed movie model that represents a movie based on the sample data present in the testing suite.
    /// To be used for performance tests for inserting a lot of documents.
    /// </summary>
    public class Movie
    {
        [Id]
        public string TmdbId { get; set; }

        [PartitionKey]
        public string Title { get; set; }
        public string Tagline { get; set; }
        public string Overview { get; set; }

        public DateTime ReleaseDate { get; set; }

        public int Runtime { get; set; }
        public long Budget { get; set; }
        public long Revenue { get; set; }

        public string Language { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Keywords { get; set; }

        public decimal AvgRating { get; set; }
        public int Votes { get; set; }

        public MovieFormat Format { get; set; }
    }

}
