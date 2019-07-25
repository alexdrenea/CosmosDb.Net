using CosmosDb.Sample.Shared.Models.Csv;
using CosmosDb.Sample.Shared.Models.Graph;
using CosmosDb.Sample.Shared.Models.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CosmosDb.Sample.Shared.Models
{
    public static class Mapper
    {
        public static Movie ToMovie(this MovieCsv movieCsv)
        {
            return new Movie
            {
                TmdbId = movieCsv.TmdbId,
                Budget = movieCsv.Budget,
                Genres = movieCsv.Genres.Split(';').ToList(),
                Keywords = movieCsv.Keywords.Split(';').ToList(),
                Language = movieCsv.Language,
                Overview = movieCsv.Overview,
                AvgRating = movieCsv.Rating,
                Votes = movieCsv.Votes,
                ReleaseDate = movieCsv.ReleaseDate,
                Revenue = movieCsv.Revenue,
                Runtime = movieCsv.Runtime,
                Tagline = movieCsv.Tagline,
                Title = movieCsv.Title
            };
        }

        public static MovieVertex ToMovieVertex(this MovieCsv movieCsv)
        {
            return new MovieVertex
            {
                TmdbId = movieCsv.TmdbId,
                Budget = movieCsv.Budget,
                Genres = movieCsv.Genres.Split(';').ToList(),
                Keywords = movieCsv.Keywords.Split(';').ToList(),
                Language = movieCsv.Language,
                Overview = movieCsv.Overview,
                AvgRating = movieCsv.Rating,
                Votes = movieCsv.Votes,
                ReleaseDate = movieCsv.ReleaseDate,
                Revenue = movieCsv.Revenue,
                Runtime = movieCsv.Runtime,
                Tagline = movieCsv.Tagline,
                Title = movieCsv.Title
            };
        }

        public static Cast ToCast(this CastCsv castCsv, string movieTitle)
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

        public static CastVertex ToCastVertex(this CastCsv castCsv, string movieTitle)
        {
            return new CastVertex
            {
                MovieTitle = movieTitle,
                MovieId = castCsv.TmdbId,
                ActorName = castCsv.Name,
                Character = castCsv.Character,
                Order = castCsv.Order,
                Uncredited = castCsv.Uncredited,
            };
        }

        public static IEnumerable<KeywordVertex> AllKeywords(this IEnumerable<MovieCsv> movies)
        {
            return movies.SelectMany(m => m.Keywords.Split(';'))
                         .Select(k => k.Trim().ToLower())
                         .Distinct()
                         .Select(k => new KeywordVertex { Keyword = k })
                         .ToArray();
        }

        public static IEnumerable<GenreVertex> AllGenres(this IEnumerable<MovieCsv> movies)
        {
            return movies.SelectMany(m => m.Genres.Split(';'))
                         .Select(k => k.Trim().ToLower())
                         .Distinct()
                         .Select(g => new GenreVertex { Genre = g })
                         .ToArray();
        }
    }
}
