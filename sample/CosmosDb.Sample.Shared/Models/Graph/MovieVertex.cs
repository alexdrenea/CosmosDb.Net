using CosmosDb.Attributes;
using System;
using System.Collections.Generic;

namespace CosmosDb.Sample.Shared.Models.Graph
{
    public enum MovieFormat
    {
        Regular,
        Imax,
        _3D
    }

    public class MovieVertex
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
