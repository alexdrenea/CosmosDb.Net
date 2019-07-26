using CosmosDb.Domain;
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
#if !DEBUG
    [Ignore("Don't run on CI since it requries a conection to a CosmosDB. Update account name and key and run locally only")]
#endif
    public class CosmosClientGremlinTests
    {
        private static string accountName = "f0887a1a-0ee0-4-231-b9ee";
        private static string accountKey = "whnySIy325FgHd9h6iex6i0IRZ0QsJYRlmAjzURFD468TPYuh4jA9DfUFwNfXReJq85S54pUnxXJknWFczQvNw==";

        private static string databaseId = "core";
        private static string containerId = "test1";

        private static string moviesTestDataPath = "TestData/Samples/movies_lite.csv";
        private static string castTestDataPath = "TestData/Samples/movies_cast_lite.csv";


        private static List<MovieFullGraph> _movies;
        private static Dictionary<string, List<Cast>> _cast;
        private static ICosmosClientGraph _cosmosClient;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var moviesCsv = Helpers.GetFromCsv<MovieCsv>(moviesTestDataPath);
            var castCsv = Helpers.GetFromCsv<CastCsv>(castTestDataPath);

            _movies = moviesCsv.Select(MovieFullGraph.GetMovieFullGraph).ToList();
            var moviesDic = _movies.ToDictionary(k => k.TmdbId);
            _cast = castCsv.Select(c => Cast.GetCastFromCsv(c)).GroupBy(k => k.MovieId).ToDictionary(k => k.Key, v => v.ToList());

            Assert.AreEqual(4802, moviesCsv.Count());

            _cosmosClient = await CosmosClientGraph.GetClientWithSql(accountName, accountKey, databaseId, containerId);
        }

        [TestMethod]
        [Priority(1)]

        public async Task InsertVertex()
        {
            var movie = _movies.ElementAt(0);

            var insert = await _cosmosClient.InsertVertex(movie);
            Assert.IsTrue(insert.IsSuccessful);

            var read = await _cosmosClient.ReadVertex<MovieFullGraph>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            var insert2 = await _cosmosClient.InsertVertex(movie);
            Assert.IsFalse(insert2.IsSuccessful, "Insert with same id should fail");

            Helpers.AssertMovieFullIsSame(movie, read.Result);
        }

        [TestMethod]
        [Priority(2)]
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
        [Priority(3)]
        public async Task InsertManyCosmosvertices()
        {
            var insert = await _cosmosClient.InsertVertex(_movies.Skip(10).Take(100), (partial) => { Console.WriteLine($"inserted {partial.Count()} vertices"); });

            var totalRu = insert.Sum(i => i.RequestCharge);
            var totalTime = insert.Sum(i => i.ExecutionTime.TotalSeconds);

            Assert.IsTrue(insert.All(i => i.IsSuccessful));
        }

        [TestMethod]
        [Priority(4)]
        public async Task UpsertManyCosmosvertices()
        {
            var movies = _movies.Take(10);
            var cast = movies.SelectMany(m => _cast[m.TmdbId]).ToList();

            var upsertMovies = await _cosmosClient.UpsertVertex(movies, (partial) => { Console.WriteLine($"upserted {partial.Count()} vertices"); });
            var upsertCast = await _cosmosClient.UpsertVertex(cast, (partial) => { Console.WriteLine($"upserted {partial.Count()} vertices"); });

            var totalRu = upsertMovies.Sum(i => i.RequestCharge) + upsertCast.Sum(i => i.RequestCharge);
            var totalTime = upsertMovies.Sum(i => i.ExecutionTime.TotalSeconds) + upsertCast.Sum(i => i.ExecutionTime.TotalSeconds);

            //Yes, Total Seconds is corert. If you're running multiple threads, adding the execution time of each operation will result in a number much higher than the atual time it takes to execute the method
            // i.e for 4 threads, the total time measured by adding all timings will be ~4 times higher (cause we're running in parralel)
            Assert.IsTrue(upsertMovies.All(i => i.IsSuccessful));
            Assert.IsTrue(upsertCast.All(i => i.IsSuccessful));
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


        //TODO: test some weird traversals .tree(), .path(), etc.

        [TestMethod]
        public async Task InsertEdgeSingleWithVertexReference()
        {
            var movie = _movies.ElementAt(0);
            var cast = _cast[movie.TmdbId];
            var cast1 = cast.ElementAt(0);

            var edge = await _cosmosClient.InsertEdge(new MovieCastEdge() { Order = cast1.Order }, movie, cast1, single: true);
            Assert.IsTrue(edge.IsSuccessful);

            var testEdge = await _cosmosClient.ExecuteGremlinSingle<Cast>($"g.V().hasId('{movie.TmdbId}').out()");
            Assert.IsTrue(testEdge.IsSuccessful);
            Helpers.AssertCastIsSame(cast1, testEdge.Result);

            var edge2 = await _cosmosClient.InsertEdge(new MovieCastEdge() { Order = cast1.Order }, movie, cast1, single: true);
            Assert.IsFalse(edge2.IsSuccessful);
        }

        [TestMethod]
        public async Task InsertEdgeMultiWithVertexReference()
        {
            var movie = _movies.ElementAt(1);
            var cast = _cast[movie.TmdbId];
            var cast1 = cast.ElementAt(0);

            var edge = await _cosmosClient.InsertEdge(new MovieCastEdge() { Order = cast1.Order }, movie, cast1, single: false);
            Assert.IsTrue(edge.IsSuccessful);

            var testEdge = await _cosmosClient.ExecuteGremlinSingle<Cast>($"g.V().hasId('{movie.TmdbId}').out()");
            Assert.IsTrue(testEdge.IsSuccessful);
            Helpers.AssertCastIsSame(cast1, testEdge.Result);

            var edge2 = await _cosmosClient.InsertEdge(new MovieCastEdge() { Order = cast1.Order }, movie, cast1, single: false);
            Assert.IsTrue(edge2.IsSuccessful);

            var testEdges = await _cosmosClient.ExecuteGremlin<Cast>($"g.V().hasId('{movie.TmdbId}').out()");
            Assert.IsTrue(testEdges.IsSuccessful);
            Assert.AreEqual(2, testEdges.Result.Count());
        }

        [TestMethod]
        public async Task InsertEdgeSingleWithGraphItemBase()
        {
            var movie = _movies.ElementAt(2);
            var cast = _cast[movie.TmdbId];
            var cast1 = cast.ElementAt(0);

            var edge = await _cosmosClient.InsertEdge(new MovieCastEdge() { Order = cast1.Order }, new GraphItemBase(movie.TmdbId, movie.Title, movie.GetType().Name), new GraphItemBase(cast1.Id, cast1.ActorName, cast1.GetType().Name), single: true);
            Assert.IsTrue(edge.IsSuccessful);

            var testEdge = await _cosmosClient.ExecuteGremlinSingle<Cast>($"g.V().hasId('{movie.TmdbId}').out()");
            Assert.IsTrue(testEdge.IsSuccessful);
            Helpers.AssertCastIsSame(cast1, testEdge.Result);

            var edge2 = await _cosmosClient.InsertEdge(new MovieCastEdge() { Order = cast1.Order }, new GraphItemBase(movie.TmdbId, movie.Title, movie.GetType().Name), new GraphItemBase(cast1.Id, cast1.ActorName, cast1.GetType().Name), single: true);
            Assert.IsFalse(edge2.IsSuccessful);
        }

        [TestMethod]
        public async Task InsertEdgeMultiWithGraphItemBase()
        {
            var movie = _movies.ElementAt(3);
            var cast = _cast[movie.TmdbId];
            var cast1 = cast.ElementAt(0);

            var edge = await _cosmosClient.InsertEdge(new MovieCastEdge() { Order = cast1.Order }, new GraphItemBase(movie.TmdbId, movie.Title, movie.GetType().Name), new GraphItemBase(cast1.Id, cast1.ActorName, cast1.GetType().Name), single: false);
            Assert.IsTrue(edge.IsSuccessful);

            var testEdge = await _cosmosClient.ExecuteGremlinSingle<Cast>($"g.V().hasId('{movie.TmdbId}').out()");
            Assert.IsTrue(testEdge.IsSuccessful);
            Helpers.AssertCastIsSame(cast1, testEdge.Result);

            var edge2 = await _cosmosClient.InsertEdge(new MovieCastEdge() { Order = cast1.Order }, new GraphItemBase(movie.TmdbId, movie.Title, movie.GetType().Name), new GraphItemBase(cast1.Id, cast1.ActorName, cast1.GetType().Name), single: false);
            Assert.IsTrue(edge2.IsSuccessful);

            var testEdges = await _cosmosClient.ExecuteGremlin<Cast>($"g.V().hasId('{movie.TmdbId}').out()");
            Assert.IsTrue(testEdges.IsSuccessful);
            Assert.AreEqual(2, testEdges.Result.Count());
        }




        [TestMethod]
        [Priority(100)]
        public async Task TestIdInvalidIdCharacters()
        {
            //https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.documents.resource.id?redirectedfrom=MSDN&view=azure-dotnet#overloads
            var good = new TestModel { Id = "good-id", Pk = "good-partition" };
            var withSpace = new TestModel { Id = "id with space", Pk = "good-partition" };
            var withSlash = new TestModel { Id = "id-with-/", Pk = "good-partition" };
            var withBackslash = new TestModel { Id = "id-with-\\", Pk = "good-partition" };
            var withHash = new TestModel { Id = "id-with-#", Pk = "good-partition" };
            var withDollar = new TestModel { Id = "id-with-$", Pk = "good-partition" };

            var insertGood = await _cosmosClient.UpsertVertex(good);
            var insertwithSpace = await _cosmosClient.UpsertVertex(withSpace);
            var insertwithSlash = await _cosmosClient.UpsertVertex(withSlash);
            var insertwithBackslash = await _cosmosClient.UpsertVertex(withBackslash);
            var insertwithHash = await _cosmosClient.UpsertVertex(withHash);
            var insertwithDollar = await _cosmosClient.UpsertVertex(withDollar);

            Assert.IsTrue(insertGood.IsSuccessful);
            Assert.IsTrue(insertwithSpace.IsSuccessful);
            Assert.IsTrue(insertwithSlash.IsSuccessful);
            Assert.IsTrue(insertwithBackslash.IsSuccessful);
            Assert.IsTrue(insertwithHash.IsSuccessful);
            Assert.IsTrue(insertwithDollar.IsSuccessful);
        }


        [TestMethod]
        [Priority(100)]
        public async Task TestIdInvalidPkCharacters()
        {
            //https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.documents.resource.id?redirectedfrom=MSDN&view=azure-dotnet#overloads
            var good = new TestModel { Id = "good-id", Pk = "good-partition" };
            var withSpace = new TestModel { Id = "good-id", Pk = "partition with space" };
            var withSlash = new TestModel { Id = "good-id", Pk = "good-partition-with-/" };
            var withBackslash = new TestModel { Id = "good-id", Pk = "good-partitionwith-\\" };
            var withHash = new TestModel { Id = "good-id", Pk = "good-partition-with-#" };
            var withDollar = new TestModel { Id = "good-id", Pk = "good-partition-with-$" };

            var insertGood = await _cosmosClient.UpsertVertex(good);
            var insertwithSpace = await _cosmosClient.UpsertVertex(withSpace);
            var insertwithSlash = await _cosmosClient.UpsertVertex(withSlash);
            var insertwithBackslash = await _cosmosClient.UpsertVertex(withBackslash);
            var insertwithHash = await _cosmosClient.UpsertVertex(withHash);
            var insertwithDollar = await _cosmosClient.UpsertVertex(withDollar);

            Assert.IsTrue(insertGood.IsSuccessful);
            Assert.IsTrue(insertwithSpace.IsSuccessful);
            Assert.IsTrue(insertwithSlash.IsSuccessful);
            Assert.IsTrue(insertwithBackslash.IsSuccessful);
            Assert.IsTrue(insertwithHash.IsSuccessful);
            Assert.IsTrue(insertwithDollar.IsSuccessful);
        }
    }
}
