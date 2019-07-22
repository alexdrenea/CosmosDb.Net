using CosmosDb.Tests.TestData;
using CosmosDb.Tests.TestData.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDb.Tests
{
    [TestClass]
    public class CosmosSqlTests
    {
        private static List<MovieCsv> _movies;
        private static List<CastCsv> _cast;
        private static ICosmosSqlClient _cosmosClient;

        private static string cosmosSqlConnectionString = "AccountEndpoint=https://mlsdatabasesql.documents.azure.com:443/;AccountKey=YdpG8nEhoeSXZjHoD9d4h4UJUEFyLJu89PqM7zqm9EBHjF6FXedA2nKAZTqmhJ7zcGHzJAv2WC3BnNXNBl9yJg==;";

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            _movies = Helpers.GetFromCsv<MovieCsv>("TestData/Samples/movies_lite.csv");
            _cast = Helpers.GetFromCsv<CastCsv>("TestData/Samples/movies_cast_lite.csv");

            Assert.AreEqual(4802, _movies.Count());
            Assert.AreEqual(106257, _cast.Count());

            _cosmosClient = await CosmosSqlClient.GetCosmosDbClient(cosmosSqlConnectionString, "test", "test1", forceCreate: false);
        }

        [TestMethod]
        public async Task InsertCosmosDocument()
        {
            var movie = _movies.First();
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();
            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var insert = await _cosmosClient.InsertDocument(movieFull);
            Assert.IsTrue(insert.IsSuccessful);

            var read = await _cosmosClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            var insert2 = await _cosmosClient.InsertDocument(movieFull);
            Assert.IsFalse(insert2.IsSuccessful, "Insert with same id should fail");

            Helpers.AssertMovieFullIsSame(movieFull, read.Result);
        }

        [TestMethod]
        public async Task UpsertCosmosDocument()
        {
            var movie = _movies.ElementAt(1);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();
            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var upsert = await _cosmosClient.UpsertDocument(movieFull);
            Assert.IsTrue(upsert.IsSuccessful);

            var read = await _cosmosClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            Helpers.AssertMovieFullIsSame(movieFull, read.Result);

            movieFull.Budget += 1;

            var upsert2 = await _cosmosClient.UpsertDocument(movieFull);
            Assert.IsTrue(upsert2.IsSuccessful);
            var read2 = await _cosmosClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            Helpers.AssertMovieFullIsSame(movieFull, read2.Result);
        }

        [TestMethod]
        public async Task ReadDocument()
        {
            var movie = _movies.ElementAt(0);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var read = await _cosmosClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            Helpers.AssertMovieFullIsSame(movieFull, read.Result);
        }

        [TestMethod]
        public async Task ExecuteSql()
        {
            var movie = _movies.ElementAt(0);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var read = await _cosmosClient.ExecuteSQL<MovieFull>("select * from c where c.Title = 'Avatar'");
            Assert.IsTrue(read.IsSuccessful);
        }

        [TestMethod]
        public async Task ExecuteSqlSpecificParameters()
        {
            var movie = _movies.ElementAt(0);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var read = await _cosmosClient.ExecuteSQL<MovieFull>("select c.Title, c.Tagline, c.Overview from c");
            Assert.IsTrue(read.IsSuccessful);
        }

        //TODO: Test with continuation

        //TODO: test with SQL not with *
    }
}
