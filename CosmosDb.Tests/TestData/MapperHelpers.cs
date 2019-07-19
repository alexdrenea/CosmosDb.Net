using CosmosDb.Tests.TestData.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CosmosDb.Tests.TestData
{
    public static class MapperHelpers
    {
        public static MovieFull GetMovieFull(MovieCsv movieCsv, IEnumerable<CastCsv> cast)
        {
            return new MovieFull
            {
                TmdbId = movieCsv.TmdbId,
                Budget = movieCsv.Budget,
                Cast = cast.Select(c => new Cast { Character = c.Character, Name = c.Name, Order = c.Order, Uncredited = c.Uncredited }).ToList(),
                Genres = movieCsv.Genres,
                Keywords = movieCsv.Keywords,
                Language = movieCsv.Language,
                Overview = movieCsv.Overview,
                Rating = new Rating { SiteName = "TvDB", MaxRating = 5, AvgRating = movieCsv.Rating, Votes = movieCsv.Votes },
                ReleaseDate = movieCsv.ReleaseDate,
                Revenue = movieCsv.Revenue,
                Runtime = movieCsv.Runtime,
                Tagline = movieCsv.Tagline,
                Title = movieCsv.Title
            };
        }
    }
}
