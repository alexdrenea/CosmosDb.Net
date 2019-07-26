using CosmosDb.Domain;
using CosmosDb.Sample.Shared;
using CosmosDb.Sample.Shared.Models;
using CosmosDb.Sample.Shared.Models.Graph;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDb.Sample.GraphConsole
{
    class Program
    {
        private static string _accountName;
        private static string _accountKey;
        private static string _databaseId;
        private static string _containerId;
        private static ICosmosClientGraph _cosmosClient;

        public static async Task Main(string[] args)
        {
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .AddJsonFile("appSettings.json")
                    .Build();

                _accountName = GetConfigValueOrDefault<string>(configuration, "AccountName", true);
                _accountKey = GetConfigValueOrDefault<string>(configuration, "AccountKey", true);
                _databaseId = GetConfigValueOrDefault<string>(configuration, "DatabaseId", true);
                _containerId = GetConfigValueOrDefault<string>(configuration, "ContainerId", true);

                _cosmosClient = await CosmosClientGraph.GetClientWithSql(_accountName, _accountKey, _databaseId, _containerId);

                await new ConsoleREPL(new Program()).RunLoop();
            }
            catch (CosmosException cre)
            {
                Console.WriteLine(cre.ToString());
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        [ConsoleActionTrigger("lm","loadmovies")]
        [ConsoleActionDescription("Load Movies bulk. Parameters: number of movies to load (defaults to All), number of Threads (defaults to 4).")]
        [ConsoleActionDisplayOrder(10)]
        public async Task LoadMovies(string parameter = "")
        {
            //Load sample data
            var moviesCsv = DataLoader.LoadMovies();
            var movies = moviesCsv.Select(m => m.ToMovieVertex());
            var genres = moviesCsv.AllGenres().ToList();

            //Parse parameters
            var options = parameter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), movies.Count()) : movies.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads

            //Upsert movies
            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {numebrOfRecords} movies (using {threads} threads)...");
            var upsertResult = await _cosmosClient.UpsertVertex(movies.Take(numebrOfRecords), (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{numebrOfRecords} movies"); }, threads);
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("lk", "loadkeywords")]
        [ConsoleActionDescription("Load Keywords bulk. Parameters: number of keywords to load (defaults to All), number of threads (defaults to 4).")]
        [ConsoleActionDisplayOrder(20)]
        public async Task LoadKeywords(string parameter = "")
        {
            //Load sample data;
            var keywords = DataLoader.LoadMovies().AllKeywords().ToList();
            //Parse parameters
            var options = parameter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), keywords.Count()) : keywords.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads

            //Upsert keywords
            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {numebrOfRecords} keywords (using {threads} threads)...");
            var upsertResult = await _cosmosClient.UpsertVertex(keywords.Take(numebrOfRecords), (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{numebrOfRecords} keywords"); }, threads);
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("lks","loadkeywordsseq")]
        [ConsoleActionDescription("Load Keywords sequentially (await each insert). Optional parameters number of records.")]
        [ConsoleActionDisplayOrder(21)]
        public async Task LoadKeywordsSquential(string parameter = "")
        {
            //Load sample data;
            var keywords = DataLoader.LoadMovies().AllKeywords().ToList();

            //Parse parameters
            var options = parameter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), keywords.Count()) : keywords.Count(); // default to all

            //Upsert keywords
            var startTime = DateTime.Now;
            var upsertResult = new List<CosmosResponse>();
            ConsoleHelpers.ConsoleLine($"Inserting {numebrOfRecords} keywords one at a time...");
            foreach (var k in keywords.Take(numebrOfRecords))
                upsertResult.Add(await _cosmosClient.UpsertVertex(k));
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("lg", "loadgenres")]
        [ConsoleActionDescription("Load Genres.")]
        [ConsoleActionDisplayOrder(30)]
        public async Task LoadGenres(string parameter = "")
        {
            //Load sample data
            var genres = DataLoader.LoadMovies().AllGenres().ToList();
            var vals = genres.Select(g => g.Genre);

            var startTime = DateTime.Now;

            var tasks = genres.Select(g => _cosmosClient.UpsertVertex(g));
            await Task.WhenAll(tasks);

            var upsertResult = tasks.Select(t => t.Result).ToList();
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("le", "loadedges")]
        [ConsoleActionDescription("Load all edges (movie-keyword, movie-genre). Optional parameters number of records.")]
        [ConsoleActionDisplayOrder(40)]
        public async Task LoadEdges(string parameter)
        {
            var moviesCsv = DataLoader.LoadMovies();

            //Parse parameters
            var options = parameter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), moviesCsv.Count()) : moviesCsv.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads

            var movies = moviesCsv.Take(numebrOfRecords).Select(m => m.ToMovieVertex());
            var genres = moviesCsv.AllGenres().ToDictionary(g => g.Genre);
            var keywords = moviesCsv.AllKeywords().ToDictionary(k => k.Keyword);

            //Generate Keyword edges. Each movie has multiple keywords so for each movie we need to generate one for each keyword it has.
            var keywordEdgeDefinitions = movies.SelectMany(m => {
                var mgib = _cosmosClient.CosmosSerializer.ToGraphItemBase(m);
                return m.Keywords.Select(k => new EdgeDefinition(new MovieKeywordEdge(), mgib, _cosmosClient.CosmosSerializer.ToGraphItemBase(keywords[k]), true));
            }).ToArray();

            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {keywordEdgeDefinitions.Count()} keywordEdges (using {threads} threads)...");
            var upsertKeywordEdges = await _cosmosClient.UpsertEdges(keywordEdgeDefinitions, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{keywordEdgeDefinitions.Count()} keywordEdges"); }, threads);
            ConsoleHelpers.PrintStats(upsertKeywordEdges, DateTime.Now.Subtract(startTime).TotalSeconds);

            //Generate genre edges. Each movie has multiple keywords so for each movie we need to generate one for each genre it has.
            var genreEdgeDefinitions = movies.SelectMany(m => {
                var mgib = _cosmosClient.CosmosSerializer.ToGraphItemBase(m);
                return m.Genres.Where(g=>!string.IsNullOrEmpty(g)).Select(g => new EdgeDefinition(new MovieGenreEdge(), mgib, _cosmosClient.CosmosSerializer.ToGraphItemBase(genres[g]), true));
            }).ToArray();

            var startTime2 = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {genreEdgeDefinitions.Count()} genre Edges (using {threads} threads)...");
            var upsertGenreEdges = await _cosmosClient.UpsertEdges(genreEdgeDefinitions, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{genreEdgeDefinitions.Count()} genre Edges"); }, threads);
            ConsoleHelpers.PrintStats(upsertGenreEdges, DateTime.Now.Subtract(startTime2).TotalSeconds);
        }

        [ConsoleActionTrigger("load")]
        [ConsoleActionDescription("Load all edges (movie-keyword, movie-genre). Optional parameters number of records.")]
        [ConsoleActionDisplayOrder(50)]
        public async Task LoadGraph(string parameter)
        {
            await LoadMovies(parameter);
            await LoadKeywords(parameter);
            await LoadGenres(parameter);
            await LoadEdges(parameter);
        }

        [ConsoleActionTrigger("s.gm", "s.getmovie")]
        [ConsoleActionDescription("Retrieve a movie by title, using the SQL API.")]
        [ConsoleActionDisplayOrder(60)]
        public async Task GetMovieByTitleSql(string parameter)
        {
            var movies = await _cosmosClient.ExecuteSQL<MovieVertex>($"select * from c where c.label = 'Movie' and c.title = '{parameter}'");
            ConsoleHelpers.ConsoleLine(movies.Result);
            ConsoleHelpers.ConsoleLine($"Success: {movies.IsSuccessful}. Execution Time: {movies.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {movies.RequestCharge} RUs");
        }

        [ConsoleActionTrigger("g.gm", "g.getmovie")]
        [ConsoleActionDescription("Retrieve a movie by title, using the Gremlin")]
        [ConsoleActionDisplayOrder(70)]
        public async Task GetMovieByTitleGraph(string parameter)
        {
            var movies = await _cosmosClient.ExecuteGremlinSingle<MovieVertex>($"g.V().hasLabel('Movie').has('Title','{parameter}'");
            ConsoleHelpers.ConsoleLine(movies.Result);
            ConsoleHelpers.ConsoleLine($"Success: {movies.IsSuccessful}. Execution Time: {movies.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {movies.RequestCharge} RUs");
        }

        [ConsoleActionTrigger("q", "sql")]
        [ConsoleActionDescription("Run a query")]
        [ConsoleActionDisplayOrder(200)]
        public async Task ExecuteSql(string parameter)
        {
            var result = await _cosmosClient.ExecuteSQL<MovieVertex>(parameter);

            ConsoleHelpers.ConsoleLine($"{result.Result?.Count()} results.");
            ConsoleHelpers.ConsoleLine(result.Result?.First());
            ConsoleHelpers.ConsoleLine($"Success: {result.IsSuccessful}. Execution Time: {result.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {result.RequestCharge.ToString("#.##")} RUs");
        }

        [ConsoleActionTrigger("g", "gremlin")]
        [ConsoleActionDescription("Run a gremlin traversal")]
        [ConsoleActionDisplayOrder(201)]
        public async Task ExecuteGremlin(string parameter)
        {
            var result = await _cosmosClient.ExecuteGremlin<JObject>(parameter);

            ConsoleHelpers.ConsoleLine($"{result.Result?.Count()} results.");
            ConsoleHelpers.ConsoleLine(result.Result?.First());
            ConsoleHelpers.ConsoleLine($"Success: {result.IsSuccessful}. Execution Time: {result.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {result.RequestCharge.ToString("#.##")} RUs");
        }

      
        #region Helpers

        private static T GetConfigValueOrDefault<T>(IConfiguration config, string configKey, bool mandatory = false)
        {
            var value = config[configKey];
            try
            {
                if (value == null) throw new Exception();
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                if (mandatory)
                    throw new ArgumentException($"Please specify a valid '{configKey}' in the appSettings.json");

                return default(T);
            }
        }

        #endregion
    }

}
