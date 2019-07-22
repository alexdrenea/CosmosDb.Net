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
        public static async Task LoadSampleData(TestContext context)
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
            var insert = await _cosmosClient.CosmosSqlClient.InsertDocument(movieFull);
            //var read = await _cosmosClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);

            // insert again -> expect error
        }

        [TestMethod]
        public async Task UpsertVertex()
        {
            var movie = _movies.ElementAt(1);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MovieFull.GetMovieFull(movie, cast);
            var upsert = await _cosmosClient.CosmosSqlClient.UpsertDocument(movieFull);

            // var read = await _cosmosClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);

            //update movieFull property
            //upsert
            //read again, confirm change

            
        }

        //TODO: upsert with ID change -> fail
        //TODO: upasert with pk change -> fail

        [TestMethod]
        public async Task ReadDocument()
        {
            var movie = _movies.ElementAt(0);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var read = await _cosmosClient.CosmosSqlClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);
        }

        [TestMethod]
        public async Task ReadWithGremlin()
        {
            var movie = _movies.ElementAt(0);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var read = await _cosmosClient.ExecuteGremlingSingle<MovieFull>($"g.V().hasId('{movie.TmdbId}').has('PartitionKey', '{movie.Title}')");
            //var read = await _cosmosClient.ExecuteGremlingSingle<MovieFull>($"g.V()");
        }


        [TestMethod]
        public async Task ReadMultiWithGremlin()
        {
            var movie = _movies.ElementAt(0);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var read = await _cosmosClient.ExecuteGremlingMulti<MovieFull>($"g.V()");
            //var read = await _cosmosClient.ExecuteGremlingSingle<MovieFull>($"g.V()");
        }

        [TestMethod]
        public async Task ReadMultiWithSql()
        {
            var movie = _movies.ElementAt(0);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MovieFull.GetMovieFull(movie, cast);

            var read = await _cosmosClient.CosmosSqlClient.ExecuteSQL<MovieFull>($"select * from c where c.label = 'MovieFull'");
        }

        //TODO: test for reading data from sql

        //TODO: test for reading data from graph
    }
}
