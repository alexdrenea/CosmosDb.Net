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
using System.Threading;
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

        private const int NUMBER_OF_THREADS = 8;

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

                _graphClient = await CosmosClientGraph.GetClientWithSql(_accountName, _accountKey, _databaseId, _containerId, new CreateOptions(_databaseId, _containerId));

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

        [ConsoleActionTrigger("lm", "loadmovies")]
        [ConsoleActionDescription("Load Movies bulk. Parameters: number of movies to load (defaults to All), number of Threads (defaults to 8).")]
        [ConsoleActionDisplayOrder(10)]
        public async Task LoadMovies(string parameter = "")
        {
            //Load sample data
            var movies = DataLoader.LoadMovies().Select(m => m.ToMovie());

            //Parse parameters
            var args = Parse2intParameters(parameter, movies.Count());

            //Upsert movies
            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {args.records} movies (using {args.threads} threads)...");
            var upsertResult = await _graphClient.UpsertVertex(movies.Take(args.records), (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{args.records} movies"); }, threads: args.threads);
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("lc", "loadcast")]
        [ConsoleActionDescription("Load Cast bulk. Parameters: mumber of movies to load cast for (defaults to All), number of Threads (defaults to 8).")]
        [ConsoleActionDisplayOrder(10)]
        public async Task LoadCast(string parameter = "")
        {
            //Load sample data
            var movies = DataLoader.LoadMovies().Select(m => m.ToMovie());

            //Parse parameters
            var args = Parse2intParameters(parameter, movies.Count());

            //Load Samples data
            var allCast = DataLoader.LoadCast().Select(c => c.ToCast()).GroupBy(c => c.MovieId).ToDictionary(k => k.Key, v => v);
            var castToLoad = movies.Take(args.records).SelectMany(m => allCast[m.TmdbId]);

            //Upsert cast
            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {castToLoad.Count()} cast (using {args.threads} threads)...");
            var upsertResult = await _graphClient.UpsertVertex(castToLoad, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{castToLoad.Count()} cast"); }, threads: args.threads);
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("la", "loadactors")]
        [ConsoleActionDescription("Load Actors bulk. Parameters: mumber of movies to load actors for (defaults to All), number of Threads (defaults to 8).")]
        [ConsoleActionDisplayOrder(10)]
        public async Task LoadActors(string parameter = "")
        {
            //Load sample data
            var movies = DataLoader.LoadMovies().Select(m => m.ToMovie());

            //Parse parameters
            var args = Parse2intParameters(parameter, movies.Count());

            var allCast = DataLoader.LoadCast().Select(c => c.ToCast()).GroupBy(c => c.MovieId).ToDictionary(k => k.Key, v => v);
            var castToLoad = movies.Take(args.records).SelectMany(m => allCast[m.TmdbId]);
            var actors = castToLoad.AllActors();

            //Upsert actors
            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {actors.Count()} actors (using {args.threads} threads)...");
            var upsertResult = await _graphClient.UpsertVertex(actors, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{actors.Count()} actors"); }, threads: args.threads);
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("lk", "loadkeywords")]
        [ConsoleActionDescription("Load Keywords bulk. Parameters: number of movies to load keywords from (defaults to All), number of threads (defaults to 8).")]
        [ConsoleActionDisplayOrder(20)]
        public async Task LoadKeywords(string parameter = "")
        {
            //Load sample data;
            var movies = DataLoader.LoadMovies();

            //Parse parameters
            var args = Parse2intParameters(parameter, movies.Count());

            var keywords = movies.Take(args.records).AllKeywords();

            //Upsert keywords
            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {keywords.Count()} keywords (using {args.threads} threads)...");
            var upsertResult = await _graphClient.UpsertVertex(keywords, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{keywords.Count()} keywords"); }, threads: args.threads);
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("lks", "loadkeywordsseq")]
        [ConsoleActionDescription("Load Keywords sequentially (await each insert). Optional parameters number of records.")]
        [ConsoleActionDisplayOrder(21)]
        public async Task LoadKeywordsSquential(string parameter = "")
        {
            //Load sample data;
            var movies = DataLoader.LoadMovies();

            //Parse parameters
            var args = Parse2intParameters(parameter, movies.Count());

            var keywords = movies.Take(args.records).AllKeywords();

            //Upsert keywords
            var startTime = DateTime.Now;
            var upsertResult = new List<CosmosResponse>();
            ConsoleHelpers.ConsoleLine($"Inserting {keywords.Count()} keywords one at a time...");
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
        [ConsoleActionDescription("Load all edges (movie-keyword, movie-genre, movie-cast, actor-cast). Parameters: mumber of movies to load edges for (defaults to All), number of Threads (defaults to 8).")]
        [ConsoleActionDisplayOrder(40)]
        public async Task LoadEdges(string parameter)
        {
            var moviesCsv = DataLoader.LoadMovies();

            //Parse parameters
            var args = Parse2intParameters(parameter, moviesCsv.Count());

            moviesCsv = moviesCsv.Take(args.records).ToList();

            var castCsv = DataLoader.LoadCast().GroupBy(c => c.MovieId).ToDictionary(k => k.Key, v => v);
            var movies = moviesCsv.Select(m => m.ToMovie()).ToDictionary(m => m.TmdbId);
            var cast = moviesCsv.SelectMany(m => castCsv[m.TmdbId].Select(c => c.ToCast())).ToList();
            var actors = cast.AllActors().ToDictionary(a => a.Name);
            var genres = moviesCsv.AllGenres().ToDictionary(g => g.Genre);
            var keywords = moviesCsv.AllKeywords().ToDictionary(k => k.Keyword);

            //Generate Keyword edges. Each movie has multiple keywords so for each movie we need to generate one for each keyword it has.
            var keywordEdgeDefinitions = movies.Values.SelectMany(m =>
            {
                var mgib = _graphClient.CosmosSerializer.ToGraphItemBase(m);
                return m.Keywords.Select(k => new EdgeDefinition(new MovieKeywordEdge(), mgib, _graphClient.CosmosSerializer.ToGraphItemBase(keywords[k]), true));
            }).ToArray();

            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {keywordEdgeDefinitions.Count()} keyword Edges (using {args.threads} threads)...");
            var upsertKeywordEdges = await _graphClient.UpsertEdges(keywordEdgeDefinitions, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{keywordEdgeDefinitions.Count()} keyword Edges"); }, threads: args.threads);
            ConsoleHelpers.PrintStats(upsertKeywordEdges, DateTime.Now.Subtract(startTime).TotalSeconds);
            //==================


            //Generate genre edges. Each movie has multiple genres so for each movie we need to generate one for each genre it has.
            var genreEdgeDefinitions = movies.Values.SelectMany(m =>
            {
                var mgib = _graphClient.CosmosSerializer.ToGraphItemBase(m);
                return m.Genres.Where(g => !string.IsNullOrEmpty(g)).Select(g => new EdgeDefinition(new MovieGenreEdge(), mgib, _graphClient.CosmosSerializer.ToGraphItemBase(genres[g]), true));
            }).ToArray();

            var startTime2 = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {genreEdgeDefinitions.Count()} genre Edges (using {args.threads} threads)...");
            var upsertGenreEdges = await _graphClient.UpsertEdges(genreEdgeDefinitions, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{genreEdgeDefinitions.Count()} genre Edges"); }, threads: args.threads);
            ConsoleHelpers.PrintStats(upsertGenreEdges, DateTime.Now.Subtract(startTime2).TotalSeconds);
            //==================


            //Generate reverse genre edges. Each movie has multiple genres so for each movie we need to generate one for each genre it has.
            var genreEdge2Definitions = movies.Values.SelectMany(m =>
            {
                var mgib = _graphClient.CosmosSerializer.ToGraphItemBase(m);
                return m.Genres.Where(g => !string.IsNullOrEmpty(g)).Select(g => new EdgeDefinition(new MovieCastEdge(), _graphClient.CosmosSerializer.ToGraphItemBase(genres[g]), mgib, true));
            }).ToArray();

            var startTime5 = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {genreEdge2Definitions.Count()} genre Edges (using {args.threads} threads)...");
            var upsertGenre2Edges = await _graphClient.UpsertEdges(genreEdge2Definitions, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{genreEdge2Definitions.Count()} genre Edges"); }, threads: args.threads);
            ConsoleHelpers.PrintStats(upsertGenre2Edges, DateTime.Now.Subtract(startTime5).TotalSeconds);
            //==================




            //Generate movieCast edges. Each movie has multiple keywords so for each movie we need to generate one for each genre it has.
            var castEdgeDefinitions = cast.Select(c =>
            {
                return new EdgeDefinition(new MovieCastEdge(), _graphClient.CosmosSerializer.ToGraphItemBase(movies[c.MovieId]), _graphClient.CosmosSerializer.ToGraphItemBase(c), true);
            }).ToArray();

            var startTime3 = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {castEdgeDefinitions.Count()} cast Edges (using {args.threads} threads)...");
            var upsertCastEdges = await _graphClient.UpsertEdges(castEdgeDefinitions, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{castEdgeDefinitions.Count()} cast Edges"); }, threads: args.threads);
            ConsoleHelpers.PrintStats(upsertCastEdges, DateTime.Now.Subtract(startTime3).TotalSeconds);
            //==================


            //Generate actor edges. Each movie has multiple keywords so for each movie we need to generate one for each genre it has.
            var actorEdgeDefinitions = cast.Select(c =>
            {
                return new EdgeDefinition(new ActorCastEdge(), _graphClient.CosmosSerializer.ToGraphItemBase(actors[c.ActorName]), _graphClient.CosmosSerializer.ToGraphItemBase(c), true);
            }).ToArray();

            var startTime4 = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {actorEdgeDefinitions.Count()} actor Edges (using {args.threads} threads)...");
            var upsertActorEdges = await _graphClient.UpsertEdges(actorEdgeDefinitions, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{actorEdgeDefinitions.Count()} actor Edges"); }, threads: args.threads);
            ConsoleHelpers.PrintStats(upsertActorEdges, DateTime.Now.Subtract(startTime4).TotalSeconds);
            //==================

        }

        [ConsoleActionTrigger("load")]
        [ConsoleActionDescription("Load entire graph. Movies, Cast, Actor, Keywords, Genres, Edges. Parameters: mumber of movies to load in graph (defaults to All), number of Threads (defaults to 8).")]
        [ConsoleActionDisplayOrder(50)]
        public async Task LoadGraph(string parameter)
        {
            await LoadMovies(parameter);
            await LoadCast(parameter);
            await LoadActors(parameter);
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

        [ConsoleActionTrigger("g.gb", "g.bybydget")]
        [ConsoleActionDescription("Run a gremlin traversal that retuns movies grouped by budget")]
        [ConsoleActionDisplayOrder(70)]
        public async Task GetMoviesGoupedByBudget(string parameter)
        {
            //g.V().hasLabel('Movie').group().by('Budget').by(valueMap('Title','Tagline'))
            //g.V().hasLabel('Movie').group().by('Budget').by('Title'))
            //g.V().hasLabel('Movie').group().by('Budget')


            //Traversals that don't return a specific model or scalar value, have to be called with JObject.
            var moviesGroupByResult = await _graphClient.ExecuteGremlin<JObject>($"g.V().hasLabel('Movie').group().by('Budget').by('Title')");
            
            //A groupBy Statement returns an array with one element;
            var moviesGroupBy = moviesGroupByResult.Result.First();
            //The results of the query are an array of JProperty where the Name is the value of the first by() statement and the value is an array of elements.
            var movieGroupByKeyValues = moviesGroupBy.Children<JProperty>();

            var res = movieGroupByKeyValues.ToDictionary(k => int.Parse(k.Name), v => v.Values().Select(vv=>vv.ToString()));

            ConsoleHelpers.ConsoleLine($"Success: {moviesGroupByResult.IsSuccessful}. Execution Time: {moviesGroupByResult.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {moviesGroupByResult.RequestCharge} RUs");
        }

        [ConsoleActionTrigger("q", "sql")]
        [ConsoleActionDescription("Run a query")]
        [ConsoleActionDisplayOrder(200)]
        public async Task ExecuteSql(string parameter)
        {
            var result = await _graphClient.ExecuteSQL<JObject>(parameter);

            ConsoleHelpers.ConsoleLine($"{result.Result?.Count()} results.");
            ConsoleHelpers.ConsoleLine(result.Result?.First());
            ConsoleHelpers.ConsoleLine($"Success: {result.IsSuccessful}. Execution Time: {result.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {result.RequestCharge.ToString("#.##")} RUs");
        }

        [ConsoleActionTrigger("g", "gremlin")]
        [ConsoleActionDescription("Run a gremlin traversal")]
        [ConsoleActionDisplayOrder(201)]
        public Task ExecuteGremlin(string parameter)
        {
            return RunGremlin<dynamic>(parameter);
        }

        [ConsoleActionTrigger("g1")]
        [ConsoleActionDescription("Get all movies with a given genre (parameter)")]
        [ConsoleActionDisplayOrder(200)]
        public Task ExecuteG1(string parameter)
        {
            var traversal = $"g.V().hasLabel('Genre').has('PartitionKey','Genre').has('id', '{parameter.ToLower()}').in()";
            return RunGremlin<Movie>(traversal);
        }

        [ConsoleActionTrigger("g2")]
        [ConsoleActionDescription("Get all movies with a given genre (parameter)")]
        [ConsoleActionDisplayOrder(200)]
        public Task ExecuteG2(string parameter)
        {
            var traversal = $"g.V().hasLabel('Genre').has('PartitionKey','Genre').has('id', '{parameter.ToLower()}').out()";
            return RunGremlin<Movie>(traversal);
        }


        #region Helpers

        private static async Task RunGremlin<T>(string query)
        {
            var result = await _graphClient.ExecuteGremlin<dynamic>(query);

            ConsoleHelpers.ConsoleLine($"{result.Result?.Count()} results.");
            //ConsoleHelpers.ConsoleLine(res.Result?.FirstOrDefault());
            ConsoleHelpers.ConsoleLine($"Success: {result.IsSuccessful}. Execution Time: {result.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {result.RequestCharge.ToString("#.##")} RUs");
        }

        private static (int records, int threads) Parse2intParameters(string args, int defaultFirst, int defaultSecond = NUMBER_OF_THREADS)
        {
            //Parse parameters
            var options = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), defaultFirst) : defaultFirst; // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : defaultSecond;

            return (numebrOfRecords, threads);
        }

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
