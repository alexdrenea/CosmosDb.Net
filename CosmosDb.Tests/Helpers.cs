using CosmosDb.Tests.TestData.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CosmosDb.Tests
{
    public class Helpers
    {
        public static List<T> GetFromCsv<T>(string fName)
        {
            using (var reader = new StreamReader(fName))
            {
                using (var csv = new CsvHelper.CsvReader(reader))
                {
                    csv.Configuration.ReadingExceptionOccurred = (ex) =>
                    {
                        return true;
                    };
                    return csv.GetRecords<T>().ToList();
                }
            }
        }


        public static void AssertMovieFullIsSame(MovieFull expected, MovieFull actual)
        {
            Assert.AreEqual(expected.TmdbId, actual.TmdbId, "Id not matching");
            Assert.AreEqual(expected.Title, actual.Title, "Title not matching");
            Assert.AreEqual(expected.Tagline, actual.Tagline, "Tagline not matching");
            Assert.AreEqual(expected.Runtime, actual.Runtime, "Runtime not matching");
            Assert.AreEqual(expected.Revenue, actual.Revenue, "Revenue not matching");
            Assert.AreEqual(expected.ReleaseDate, actual.ReleaseDate, "ReleaseDate not matching");
            Assert.AreEqual(expected.Overview, actual.Overview, "Overview not matching");
            Assert.AreEqual(expected.Language, actual.Language, "Language not matching");
            Assert.AreEqual(expected.Keywords, actual.Keywords, "Keywords not matching");
            Assert.AreEqual(expected.Genres, actual.Genres, "Genres not matching");
            Assert.AreEqual(expected.Format, actual.Format, "Format not matching");
            Assert.AreEqual(expected.Budget, actual.Budget, "Budget not matching");
            Assert.AreEqual(expected.Cast.Count, actual.Cast.Count, "Cast not matching");
            AssertRatingIsSame(expected.Rating, actual.Rating);
        }

        public static void AssertCastIsSame(Cast expected, Cast actual)
        {
            Assert.AreEqual(expected.MovieTitle, actual.MovieTitle, "MovieTitle not matching");
            Assert.AreEqual(expected.Id, actual.Id, "Id not matching");
            Assert.AreEqual(expected.Name, actual.Name, "Name not matching");
            Assert.AreEqual(expected.Character, actual.Character, "Character not matching");
            Assert.AreEqual(expected.Order, actual.Order, "Order not matching");
            Assert.AreEqual(expected.Uncredited, actual.Uncredited, "Uncredited not matching");
        }

        public static void AssertRatingIsSame(Rating expected, Rating actual)
        {
            Assert.AreEqual(expected.MovieTitle, actual.MovieTitle, "MovieTitle not matching");
            Assert.AreEqual(expected.MaxRating, actual.MaxRating, "MaxRating not matching");
            Assert.AreEqual(expected.SiteName, actual.SiteName, "SiteName not matching");
            Assert.AreEqual(expected.Votes, actual.Votes, "Votes not matching");
        }
    }
}
