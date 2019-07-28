using System;

namespace CosmosDb.Tests.TestData.Models
{
    /// <summary>
    /// A line of data from the cast csv sample file.
    /// </summary>
    public class CastCsv
    {
        public string MovieId { get; set; }
        public string MovieTitle { get; set; }
        public string ActorName { get; set; }
        public string Character { get; set; }
        public int Order { get; set; }
        public bool Uncredited { get; set; }
    }

    /// <summary>
    /// A line of data from the movies csv saple file
    /// </summary>
    public class MovieCsv
    {
        public string TmdbId { get; set; }
        public string Title { get; set; }
        public string Tagline { get; set; }
        public string Overview { get; set; }

        public DateTime ReleaseDate { get; set; }

        public int Runtime { get; set; }
        public long Budget { get; set; }
        public long Revenue { get; set; }


        public string Language { get; set; }
        public string Genres { get; set; }
        public string Keywords { get; set; }

        public decimal Rating { get; set; }
        public int Votes { get; set; }
    }
}
