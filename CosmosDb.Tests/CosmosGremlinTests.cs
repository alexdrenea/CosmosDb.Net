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
    public class CosmosGremlinTests
    {
        private static List<MovieCsv> _movies;
        private static List<CastCsv> _cast;
        private static ICosmosGraphClient _cosmosClient;

        private static string cosmosGraphConnectionString = "f0887a1a-0ee0-4-231-b9ee";
        private static string cosmosGraphAccountKey = "whnySIy325FgHd9h6iex6i0IRZ0QsJYRlmAjzURFD468TPYuh4jA9DfUFwNfXReJq85S54pUnxXJknWFczQvNw==";

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            _movies = Helpers.GetFromCsv<MovieCsv>("TestData/Samples/movies_lite.csv");
            _cast = Helpers.GetFromCsv<CastCsv>("TestData/Samples/movies_cast_lite.csv");

            Assert.AreEqual(4802, _movies.Count());
            Assert.AreEqual(106257, _cast.Count());

            _cosmosClient = await CosmosGraphClient.GetCosmosGraphClientWithSql(cosmosGraphConnectionString, "core", "test1", cosmosGraphAccountKey);
        }

        [TestMethod]
        public async Task InsertVertex()
        {
            var movie = _movies.First();
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();
            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var insert = await _cosmosClient.InsertVertex(movieFull);
            Assert.IsTrue(insert.IsSuccessful);

            var read = await _cosmosClient.ReadVertex<MovieFull>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            var insert2 = await _cosmosClient.InsertVertex(movieFull);
            Assert.IsFalse(insert2.IsSuccessful, "Insert with same id should fail");

            Helpers.AssertMovieFullIsSame(movieFull, read.Result);
        }

        [TestMethod]
        public async Task UpsertVertex()
        {
            var movie = _movies.ElementAt(1);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MovieFull.GetMovieFull(movie, cast);
            var upsert = await _cosmosClient.UpsertVertex(movieFull);
            Assert.IsTrue(upsert.IsSuccessful);

            var read = await _cosmosClient.ReadVertex<MovieFull>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            Helpers.AssertMovieFullIsSame(movieFull, read.Result);

            movieFull.Budget += 1;

            var upsert2 = await _cosmosClient.UpsertVertex(movieFull);
            Assert.IsTrue(upsert2.IsSuccessful);
            var read2 = await _cosmosClient.ReadVertex<MovieFull>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            Helpers.AssertMovieFullIsSame(movieFull, read2.Result);

        }

        [TestMethod]
        public async Task ReadDocument()
        {
            var movie = _movies.ElementAt(0);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();
            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var read = await _cosmosClient.ReadVertex<MovieFull>(movie.TmdbId, movie.Title);

            Helpers.AssertMovieFullIsSame(movieFull, read.Result);
        }

        [TestMethod]
        public async Task ReadWithGremlin()
        {
            var movie = _movies.ElementAt(0);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();
            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var read = await _cosmosClient.ExecuteGremlinSingle<MovieFull>($"g.V().hasId('{movie.TmdbId}').has('PartitionKey', '{movie.Title}')");

            Helpers.AssertMovieFullIsSame(movieFull, read.Result);
        }


        [TestMethod]
        public async Task ReadMultiWithGremlin()
        {
            var movie = _movies.ElementAt(0);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var read = await _cosmosClient.ExecuteGremlin<MovieFull>($"g.V()");
        }

        [TestMethod]
        public async Task ReadMultiWithSql()
        {
            var movie = _movies.ElementAt(0);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var read = await _cosmosClient.ExecuteSQL<MovieFull>($"select * from c where c.label = 'MovieFull'");
        }

        //TODO: test for reading data from sql

        //TODO: test for reading data from graph

        //TODO: test some weird traversals .tree(), .path(), etc.

        //TODO test edges
    }
}
