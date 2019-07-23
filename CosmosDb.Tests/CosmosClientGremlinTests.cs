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
    public class CosmosClientGremlinTests
    {
        private static string cosmosGraphConnectionString = "f0887a1a-0ee0-4-231-b9ee";
        private static string cosmosGraphAccountKey = "whnySIy325FgHd9h6iex6i0IRZ0QsJYRlmAjzURFD468TPYuh4jA9DfUFwNfXReJq85S54pUnxXJknWFczQvNw==";
        private static string moviesTestDataPath = "TestData/Samples/movies_lite.csv";
        private static string castTestDataPath = "TestData/Samples/movies_cast_lite.csv";


        private static List<MovieCsv> _moviesCsv;
        private static Dictionary<string, List<CastCsv>> _castCsv;
        private static List<MovieFullGraph> _movies;

        private static ICosmosClientGraph _cosmosClient;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var moviesCsv = Helpers.GetFromCsv<MovieCsv>(moviesTestDataPath);
            var castCsv = Helpers.GetFromCsv<CastCsv>(castTestDataPath).GroupBy(k => k.TmdbId).ToDictionary(k => k.Key, v => v.ToList());
            _movies = moviesCsv.Select(MovieFullGraph.GetMovieFullGraph).ToList();

            Assert.AreEqual(4802, moviesCsv.Count());

            _cosmosClient = await CosmosClientGraph.GetClientWithSql(cosmosGraphConnectionString, cosmosGraphAccountKey, "core", "test1");
            var x = (CosmosClientGraph)_cosmosClient;
        }

        [TestMethod]
        public async Task InsertVertex()
        {
            var movie = _movies.First();

            var insert = await _cosmosClient.InsertVertex(movie);
            Assert.IsTrue(insert.IsSuccessful);

            var read = await _cosmosClient.ReadVertex<MovieFullGraph>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            var insert2 = await _cosmosClient.InsertVertex(movie);
            Assert.IsFalse(insert2.IsSuccessful, "Insert with same id should fail");

            Helpers.AssertMovieFullIsSame(movie, read.Result);
        }

        [TestMethod]
        public async Task UpsertVertex()
        {
            var movie = _movies.ElementAt(1);

            var upsert = await _cosmosClient.UpsertVertex(movie);

            Assert.IsTrue(upsert.IsSuccessful);

            var read = await _cosmosClient.ReadVertex<MovieFullGraph>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            Helpers.AssertMovieFullIsSame(movie, read.Result);

            movie.Budget += 1;

            var upsert2 = await _cosmosClient.UpsertVertex(movie);
            Assert.IsTrue(upsert2.IsSuccessful);
            var read2 = await _cosmosClient.ReadVertex<MovieFullGraph>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            Helpers.AssertMovieFullIsSame(movie, read2.Result);

        }

        [TestMethod]
        public async Task ReadDocument()
        {
            var movie = _movies.First();
            var read = await _cosmosClient.ReadVertex<MovieFullGraph>(movie.TmdbId, movie.Title);

            Assert.IsTrue(read.IsSuccessful);
            Helpers.AssertMovieFullIsSame(movie, read.Result);
        }

        [TestMethod]
        public async Task ReadWithGremlin()
        {
            var movie = _movies.ElementAt(0);
            var movie2 = _movies.ElementAt(2);

            var read = await _cosmosClient.ExecuteGremlinSingle<MovieFullGraph>($"g.V().hasId('{movie.TmdbId}').has('PartitionKey', '{movie.Title}')");
            Assert.IsTrue(read.IsSuccessful);
            Helpers.AssertMovieFullIsSame(movie, read.Result);

            var read2 = await _cosmosClient.ExecuteGremlinSingle<MovieFullGraph>($"g.V().hasId('{movie2.TmdbId}').has('PartitionKey', '{movie2.Title}')");
            Assert.IsTrue(read2.IsSuccessful);
            Helpers.AssertMovieFullIsSame(movie2, read2.Result);
        }

        [TestMethod]
        public async Task ReadWithGremlinWithBindings()
        {
            var movie = _movies.ElementAt(0);
            var movie2 = _movies.ElementAt(1);

            var read = await _cosmosClient.ExecuteGremlinSingle<MovieFullGraph>(
                $"g.V().hasId(movieId).has('PartitionKey', movieTitle)",
                new Dictionary<string, object> { { "movieId", movie.TmdbId }, { "movieTitle", movie.Title } });
            Assert.IsTrue(read.IsSuccessful);
            Helpers.AssertMovieFullIsSame(movie, read.Result);

            var read2 = await _cosmosClient.ExecuteGremlinSingle<MovieFullGraph>(
               $"g.V().hasId(movieId).has('PartitionKey', movieTitle)",
               new Dictionary<string, object> { { "movieId", movie2.TmdbId }, { "movieTitle", movie2.Title } });
            Assert.IsTrue(read2.IsSuccessful);
            Helpers.AssertMovieFullIsSame(movie2, read2.Result);
        }

        [TestMethod]
        public async Task ReadWithGremlinWithBindingsExtraParameters()
        {
            var movie = _movies.ElementAt(0);

            var read = await _cosmosClient.ExecuteGremlinSingle<MovieFullGraph>(
                $"g.V().hasId(movieId).has('PartitionKey', movieTitle)",
                new Dictionary<string, object> { { "movieId", movie.TmdbId }, { "movieTitle", movie.Title }, { "extraparam", "something" } });
            Assert.IsTrue(read.IsSuccessful);
            Helpers.AssertMovieFullIsSame(movie, read.Result);
        }

        [TestMethod]
        public async Task ReadWithGremlinWithBindingsMissingParameters()
        {
            var movie = _movies.ElementAt(0);

            var read = await _cosmosClient.ExecuteGremlinSingle<MovieFullGraph>(
                $"g.V().hasId(movieId).has('PartitionKey', movieTitle)",
                new Dictionary<string, object> { { "movieId", movie.TmdbId } });
            Assert.IsFalse(read.IsSuccessful);
        }


        [TestMethod]
        public async Task ReadMultiWithGremlin()
        {
            var read = await _cosmosClient.ExecuteGremlin<MovieFullGraph>($"g.V()");
            Assert.IsTrue(read.IsSuccessful);
        }

        [TestMethod]
        public async Task ReadMultiWithSql()
        {
            var read = await _cosmosClient.ExecuteSQL<MovieFullGraph>($"select * from c where c.label = 'MovieFullGraph'");
            Assert.IsTrue(read.IsSuccessful);
        }

        [TestMethod]
        public async Task Insert100Cosmosvertices()
        {
            var insert = await _cosmosClient.InsertVertex(_movies.Take(100), (partial) => { Console.WriteLine($"inserted {partial.Count()} vertices"); });

            var totalRu = insert.Sum(i => i.RequestCharge);
            var totalTime = insert.Sum(i => i.ExecutionTime.TotalSeconds);

            Assert.IsTrue(insert.All(i => i.IsSuccessful));
        }

        [TestMethod]
        public async Task Upsert100Cosmosvertices()
        {
            var insert = await _cosmosClient.UpsertVertex(_movies.Take(100), (partial) => { Console.WriteLine($"upserted {partial.Count()} vertices"); });

            var totalRu = insert.Sum(i => i.RequestCharge);
            var totalTime = insert.Sum(i => i.ExecutionTime.TotalSeconds);

            Assert.IsTrue(insert.All(i => i.IsSuccessful));
        }

        //TODO: test some weird traversals .tree(), .path(), etc.

        //TODO test edges
        //TODO: test insert edge with single: true (throw exception if you try to insert another time)
         
        //TODO: test insett edge with single: false (can insert multiple)
    }
}
