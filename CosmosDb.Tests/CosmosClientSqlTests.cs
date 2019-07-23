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
    public class CosmosClientSqlTests
    {
        private static string accountName = "mlsdatabasesql";
        private static string accountKey = "YdpG8nEhoeSXZjHoD9d4h4UJUEFyLJu89PqM7zqm9EBHjF6FXedA2nKAZTqmhJ7zcGHzJAv2WC3BnNXNBl9yJg==";

        private static string accountEndpoint = $"https://{accountName}.documents.azure.com:443/";
        private static string connectionString = $"AccountEndpoint={accountEndpoint};AccountKey={accountKey};";
        private static string databaseId = "test";
        private static string containerId = "test2";


        private static string moviesTestDataPath = "TestData/Samples/movies_lite.csv";
        private static string castTestDataPath = "TestData/Samples/movies_cast_lite.csv";

        private static List<MovieFull> _movies;
        private static ICosmosClientSql _cosmosClient;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var moviesCsv = Helpers.GetFromCsv<MovieCsv>(moviesTestDataPath);
            var castCsv = Helpers.GetFromCsv<CastCsv>(castTestDataPath).GroupBy(k => k.TmdbId).ToDictionary(k => k.Key, v => v.ToList());
            //_movies = moviesCsv.Select(m => MovieFull.GetMovieFull(m, castCsv.ContainsKey(m.TmdbId) ? castCsv[m.TmdbId] : new List<CastCsv>())).ToList();
            //Don't add Cast into the movie document - testing performance vs graph
            _movies = moviesCsv.Select(m => MovieFull.GetMovieFull(m, new List<CastCsv>())).ToList();

            Assert.AreEqual(4802, moviesCsv.Count());

            _cosmosClient = await CosmosClientSql.GetByConnectionString(connectionString, databaseId, containerId, forceCreate: false);
        }

        [TestMethod]
        public async Task GetClientWithAccountName()
        {
            var ccq = CosmosClientSql.GetByAccountName(accountName, accountKey, databaseId, containerId, forceCreate: false);
            Assert.IsNotNull(ccq);

            var read = await _cosmosClient.ExecuteSQL<MovieFull>($"select * from c where c.Title = 'Avatar'");
            Assert.IsTrue(read.IsSuccessful);
        }

        [TestMethod]
        public async Task GetClientWithAccountEndpoint()
        {
            var ccq = CosmosClientSql.GetByAccountName(accountEndpoint, accountKey, databaseId, containerId, forceCreate: false);
            Assert.IsNotNull(ccq);

            var read = await _cosmosClient.ExecuteSQL<MovieFull>($"select * from c where c.Title = 'Avatar'");
            Assert.IsTrue(read.IsSuccessful);
        }

        [TestMethod]
        public async Task GetClientWithConnectionString()
        {
            var ccq = CosmosClientSql.GetByConnectionString(connectionString, databaseId, containerId, forceCreate: false);
            Assert.IsNotNull(ccq);

            var read = await _cosmosClient.ExecuteSQL<MovieFull>($"select * from c where c.Title = 'Avatar'");
            Assert.IsTrue(read.IsSuccessful);
        }


        [TestMethod]
        public async Task InsertCosmosDocument()
        {
            var movie = _movies.ElementAt(0);

            var insert = await _cosmosClient.InsertDocument(movie);
            Assert.IsTrue(insert.IsSuccessful);

            var read = await _cosmosClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            var insert2 = await _cosmosClient.InsertDocument(movie);
            Assert.IsFalse(insert2.IsSuccessful, "Insert with same id should fail");

            Helpers.AssertMovieFullIsSame(movie, read.Result);
        }

        [TestMethod]
        public async Task UpsertCosmosDocument()
        {
            var movie = _movies.ElementAt(1);

            var upsert = await _cosmosClient.UpsertDocument(movie);
            Assert.IsTrue(upsert.IsSuccessful);

            var read = await _cosmosClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            Helpers.AssertMovieFullIsSame(movie, read.Result);

            movie.Budget += 1;

            var upsert2 = await _cosmosClient.UpsertDocument(movie);
            Assert.IsTrue(upsert2.IsSuccessful);
            var read2 = await _cosmosClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            Helpers.AssertMovieFullIsSame(movie, read2.Result);
        }

        [TestMethod]
        public async Task ReadDocument()
        {
            var movie = _movies.ElementAt(0);

            var read = await _cosmosClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);
            Assert.IsTrue(read.IsSuccessful);

            Helpers.AssertMovieFullIsSame(movie, read.Result);
        }

        [TestMethod]
        public async Task ExecuteSql()
        {
            var movie = _movies.ElementAt(0);

            var read = await _cosmosClient.ExecuteSQL<MovieFull>($"select * from c where c.Title = '{movie.Title}'");

            Assert.IsTrue(read.IsSuccessful);
            Helpers.AssertMovieFullIsSame(movie, read.Result.FirstOrDefault());
        }

        [TestMethod]
        public async Task ExecuteSqlWithContinuation()
        {
            var query = $"select * from c order by c.Title";
            var readFirst = await _cosmosClient.ExecuteSQL<MovieFull>(query, true);
            Assert.IsTrue(readFirst.IsSuccessful);
            Assert.IsFalse(string.IsNullOrWhiteSpace(readFirst.ContinuationToken));

            var readnextPage = await _cosmosClient.ExecuteSQL<MovieFull>(query, true, readFirst.ContinuationToken);
            Assert.IsTrue(readnextPage.IsSuccessful);
            Assert.IsFalse(string.IsNullOrWhiteSpace(readnextPage.ContinuationToken));
        }

        [TestMethod]
        public async Task ExecuteSqlSpecificParameters()
        {
            var read = await _cosmosClient.ExecuteSQL<MovieFull>("select c.Title, c.Tagline, c.Overview from c");
            Assert.IsTrue(read.IsSuccessful);
        }

        [TestMethod]
        public async Task Insert201CosmosDocuments()
        {
            //201 items so we have 3 pages.
            var insert = await _cosmosClient.InsertDocuments(_movies.Take(201), (partial) => { Console.WriteLine($"inserted {partial.Count()} documents"); });

            var totalRu = insert.Sum(i => i.RequestCharge);
            var totalTime = insert.Sum(i => i.ExecutionTime.TotalSeconds);

            Assert.IsTrue(insert.All(i => i.IsSuccessful));
        }

        [TestMethod]
        public async Task Upsert201CosmosDocuments()
        {
            //201 items so we have 3 pages.
            var insert = await _cosmosClient.UpsertDocuments(_movies.Take(201), (partial) => { Console.WriteLine($"upserted {partial.Count()} documents"); });

            var totalRu = insert.Sum(i => i.RequestCharge);
            var totalTime = insert.Sum(i => i.ExecutionTime.TotalSeconds);

            Assert.IsTrue(insert.All(i => i.IsSuccessful));
        }

        [TestMethod]
        public async Task Upsert5000CosmosDocuments()
        {
            var insert = await _cosmosClient.UpsertDocuments(_movies.Take(5000), (partial) => { Console.WriteLine($"upserted {partial.Count()} documents"); });

            var totalRu = insert.Sum(i => i.RequestCharge);
            var totalTime = insert.Sum(i => i.ExecutionTime.TotalSeconds);

            Assert.IsTrue(insert.All(i => i.IsSuccessful));
        }

    }
}
