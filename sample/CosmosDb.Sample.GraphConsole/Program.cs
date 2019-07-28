using CosmosDb.Sample.Shared;
using CosmosDb.Sample.Shared.Models;
using CosmosDb.Sample.Shared.Models.Domain;
using CosmosDB.Net;
using CosmosDB.Net.Domain;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
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
        private static ICosmosClientGraph _graphClient;

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

                _graphClient = await CosmosClientGraph.GetClientWithSql(_accountName, _accountKey, _databaseId, _containerId, forceCreate: false);

                Console.Title = "CosmosDB.NET Gremlin Sample";
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
            var movies = moviesCsv.Select(m => m.ToMovie());
            var genres = moviesCsv.AllGenres().ToList();

            //Parse parameters
            var options = parameter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), movies.Count()) : movies.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads

            //Upsert movies
            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {numebrOfRecords} movies (using {threads} threads)...");
            var upsertResult = await _graphClient.UpsertVertex(movies.Take(numebrOfRecords), (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{numebrOfRecords} movies"); }, threads: threads);
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("lc", "loadcast")]
        [ConsoleActionDescription("Load Movies bulk. Parameters: mumber of movies to load cast for (defaults to All), number of Threads (defaults to 4).")]
        [ConsoleActionDisplayOrder(10)]
        public async Task LoadCast(string parameter = "")
        {
            //Load sample data
            var movies = DataLoader.LoadMovies().Select(m => m.ToMovie());

            //Parse parameters
            var options = parameter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), movies.Count()) : movies.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads


            var allCast = DataLoader.LoadCast().Select(c=>c.ToCast()).GroupBy(c => c.MovieId).ToDictionary(k => k.Key, v => v);
            var castToLoad = movies.Take(numebrOfRecords).SelectMany(m => allCast[m.TmdbId]);

            //Upsert cast
            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {castToLoad.Count()} cast (using {threads} threads)...");
            var upsertResult = await _graphClient.UpsertVertex(castToLoad, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{castToLoad.Count()} cast"); }, threads: threads);
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }


        [ConsoleActionTrigger("la", "loadactors")]
        [ConsoleActionDescription("Load Movies bulk. Parameters: mumber of movies to load actors for (defaults to All), number of Threads (defaults to 4).")]
        [ConsoleActionDisplayOrder(10)]
        public async Task LoadActors(string parameter = "")
        {
            //Load sample data
            var movies = DataLoader.LoadMovies().Select(m => m.ToMovie());

            //Parse parameters
            var options = parameter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), movies.Count()) : movies.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads

            var allCast = DataLoader.LoadCast().Select(c => c.ToCast()).GroupBy(c => c.MovieId).ToDictionary(k => k.Key, v => v);
            var castToLoad = movies.Take(numebrOfRecords).SelectMany(m => allCast[m.TmdbId]);
            var actors = castToLoad.AllActors();

            //Upsert cast
            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {actors.Count()} actors (using {threads} threads)...");
            var upsertResult = await _graphClient.UpsertVertex(actors, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{actors.Count()} actors"); }, threads: threads);
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("lk", "loadkeywords")]
        [ConsoleActionDescription("Load Keywords bulk. Parameters: number of movies to load keywords from (defaults to All), number of threads (defaults to 4).")]
        [ConsoleActionDisplayOrder(20)]
        public async Task LoadKeywords(string parameter = "")
        {
            //Load sample data;
            var movies = DataLoader.LoadMovies();
         
            //Parse parameters
            var options = parameter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), movies.Count()) : movies.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads

            var keywords = movies.Take(numebrOfRecords).AllKeywords();

            //Upsert keywords
            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {keywords.Count()} keywords (using {threads} threads)...");
            var upsertResult = await _graphClient.UpsertVertex(keywords, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{keywords.Count()} keywords"); }, threads: threads);
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("lks","loadkeywordsseq")]
        [ConsoleActionDescription("Load Keywords sequentially (await each insert). Optional parameters number of records.")]
        [ConsoleActionDisplayOrder(21)]
        public async Task LoadKeywordsSquential(string parameter = "")
        {
            //Load sample data;
            var movies = DataLoader.LoadMovies();

            //Parse parameters
            var options = parameter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), movies.Count()) : movies.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads

            var keywords = movies.Take(numebrOfRecords).AllKeywords();

            //Upsert keywords
            var startTime = DateTime.Now;
            var upsertResult = new List<CosmosResponse>();
            ConsoleHelpers.ConsoleLine($"Inserting {numebrOfRecords} keywords one at a time...");
            foreach (var k in keywords)
                upsertResult.Add(await _graphClient.UpsertVertex(k));
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("lg", "loadgenres")]
        [ConsoleActionDescription("Load All Genres (only 21 in the dataset, inserting them sequentially)")]
        [ConsoleActionDisplayOrder(30)]
        public async Task LoadGenres(string parameter = "")
        {
            //Load sample data
            var genres = DataLoader.LoadMovies().AllGenres().ToList();
            var vals = genres.Select(g => g.Genre);

            var startTime = DateTime.Now;

            var tasks = genres.Select(g => _graphClient.UpsertVertex(g));
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
            var cast = DataLoader.LoadCast().GroupBy(c => c.MovieId).ToDictionary(k => k.Key, v => v);

            //Parse parameters
            var options = parameter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), moviesCsv.Count()) : moviesCsv.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads

            var movies = moviesCsv.Take(numebrOfRecords).Select(m => m.ToMovie());
            var genres = moviesCsv.AllGenres().ToDictionary(g => g.Genre);
            var keywords = moviesCsv.AllKeywords().ToDictionary(k => k.Keyword);

            //Generate Keyword edges. Each movie has multiple keywords so for each movie we need to generate one for each keyword it has.
            var keywordEdgeDefinitions = movies.SelectMany(m => {
                var mgib = _graphClient.CosmosSerializer.ToGraphItemBase(m);
                return m.Keywords.Select(k => new EdgeDefinition(new MovieKeywordEdge(), mgib, _graphClient.CosmosSerializer.ToGraphItemBase(keywords[k]), true));
            }).ToArray();

            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {keywordEdgeDefinitions.Count()} keywordEdges (using {threads} threads)...");
            var upsertKeywordEdges = await _graphClient.UpsertEdges(keywordEdgeDefinitions, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{keywordEdgeDefinitions.Count()} keywordEdges"); }, threads: threads);
            ConsoleHelpers.PrintStats(upsertKeywordEdges, DateTime.Now.Subtract(startTime).TotalSeconds);

            //Generate genre edges. Each movie has multiple keywords so for each movie we need to generate one for each genre it has.
            var genreEdgeDefinitions = movies.SelectMany(m => {
                var mgib = _graphClient.CosmosSerializer.ToGraphItemBase(m);
                return m.Genres.Where(g=>!string.IsNullOrEmpty(g)).Select(g => new EdgeDefinition(new MovieGenreEdge(), mgib, _graphClient.CosmosSerializer.ToGraphItemBase(genres[g]), true));
            }).ToArray();

            var startTime2 = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {genreEdgeDefinitions.Count()} genre Edges (using {threads} threads)...");
            var upsertGenreEdges = await _graphClient.UpsertEdges(genreEdgeDefinitions, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{genreEdgeDefinitions.Count()} genre Edges"); }, threads: threads);
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
            var movies = await _graphClient.ExecuteSQL<Movie>($"select * from c where c.label = 'Movie' and c.title = '{parameter}'");
            ConsoleHelpers.ConsoleLine(movies.Result);
            ConsoleHelpers.ConsoleLine($"Success: {movies.IsSuccessful}. Execution Time: {movies.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {movies.RequestCharge} RUs");
        }

        [ConsoleActionTrigger("g.gm", "g.getmovie")]
        [ConsoleActionDescription("Retrieve a movie by title, using the Gremlin")]
        [ConsoleActionDisplayOrder(70)]
        public async Task GetMovieByTitleGraph(string parameter)
        {
            var movies = await _graphClient.ExecuteGremlinSingle<Movie>($"g.V().hasLabel('Movie').has('Title','{parameter}'");
            ConsoleHelpers.ConsoleLine(movies.Result);
            ConsoleHelpers.ConsoleLine($"Success: {movies.IsSuccessful}. Execution Time: {movies.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {movies.RequestCharge} RUs");
        }

        [ConsoleActionTrigger("q", "sql")]
        [ConsoleActionDescription("Run a query")]
        [ConsoleActionDisplayOrder(200)]
        public async Task ExecuteSql(string parameter)
        {
            var result = await _graphClient.ExecuteSQL<Movie>(parameter);

            ConsoleHelpers.ConsoleLine($"{result.Result?.Count()} results.");
            //ConsoleHelpers.ConsoleLine(result.Result?.First());
            ConsoleHelpers.ConsoleLine($"Success: {result.IsSuccessful}. Execution Time: {result.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {result.RequestCharge.ToString("#.##")} RUs");
        }

        [ConsoleActionTrigger("g", "gremlin")]
        [ConsoleActionDescription("Run a gremlin traversal")]
        [ConsoleActionDisplayOrder(201)]
        public async Task ExecuteGremlin(string parameter)
        {
            var result = await _graphClient.ExecuteGremlin<JObject>(parameter);

            ConsoleHelpers.ConsoleLine($"{result.Result?.Count()} results.");
            //ConsoleHelpers.ConsoleLine(result.Result?.First());
            ConsoleHelpers.ConsoleLine($"Success: {result.IsSuccessful}. Execution Time: {result.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {result.RequestCharge.ToString("#.##")} RUs");
        }

      
        //TODO - show example of parsing a tree()


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
