using CosmosDb.Sample.Shared.Models.Csv;
using CosmosDb.Sample.Shared.Models.Domain;
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
                Genres = movieCsv.Genres.Split(';').Select(g => g.Trim().ToLower()).ToList(),
                Keywords = movieCsv.Keywords.Split(';').Select(k => k.Trim().ToLower()).ToList(),
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

        public static Cast ToCast(this CastCsv castCsv)
        {
            return new Cast
            {
                MovieTitle = castCsv.MovieTitle,
                MovieId = castCsv.MovieId,
                ActorName = castCsv.ActorName,
                Character = castCsv.Character,
                Order = castCsv.Order,
                Uncredited = castCsv.Uncredited,
            };
        }

        public static IEnumerable<Actor> AllActors(this IEnumerable<Cast> cast)
        {
            return cast.Select(c => c.ActorName.Trim().ToLower())
                        .Distinct()
                        .Select(n => new Actor { Name = n })
                        .ToArray();
        }

        public static IEnumerable<KeywordVertex> AllKeywords(this IEnumerable<MovieCsv> movies)
        {
            return movies.SelectMany(m => m.Keywords.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                         .Select(k => k.Trim().ToLower())
                         .Distinct()
                         .Select(k => new KeywordVertex { Keyword = k })
                         .ToArray();
        }

        public static IEnumerable<GenreVertex> AllGenres(this IEnumerable<MovieCsv> movies)
        {
            return movies.SelectMany(m => m.Genres.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                         .Select(k => k.Trim().ToLower())
                         .Distinct()
                         .Select(g => new GenreVertex { Genre = g })
                         .ToArray();
        }

        public static IEnumerable<Actor> AllActors(this IEnumerable<CastCsv> cast)
        {
            return cast.Select(c => c.ActorName.Trim())
                         .Distinct()
                         .Select(a => new Actor { Name = a })
                         .ToArray();
        }
    }
}
