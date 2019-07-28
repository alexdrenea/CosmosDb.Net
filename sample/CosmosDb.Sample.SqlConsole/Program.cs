using CosmosDb.Sample.Shared;
using CosmosDb.Sample.Shared.Models;
using CosmosDb.Sample.Shared.Models.Domain;
using CosmosDB.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDb.Sample.SqlConsole
{
    class Program
    {
        private static string _accountName;
        private static string _accountKey;
        private static string _databaseId;
        private static string _containerId;

        private static ICosmosClientSql _sqlClient;

        private const int NUMBER_OF_THREADS = 8;

        public static async Task Main(string[] args)
        {
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .AddJsonFile("appSettings.json")
                    .Build();

                //Initialize Cosmos Client
                _accountName = GetConfigValueOrDefault<string>(configuration, "AccountName", true);
                _accountKey = GetConfigValueOrDefault<string>(configuration, "AccountKey", true);
                _databaseId = GetConfigValueOrDefault<string>(configuration, "DatabaseId", true);
                _containerId = GetConfigValueOrDefault<string>(configuration, "ContainerId", true);

                _sqlClient = await CosmosClientSql.GetByAccountName(_accountName, _accountKey, _databaseId, _containerId, forceCreate: false);

                Console.Title = "CosmosDB.NET SQL Sample";
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
            var upsertResult = await _sqlClient.UpsertDocuments(movies.Take(args.records), (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{args.records} movies"); }, threads: args.threads);
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
            var upsertResult = await _sqlClient.UpsertDocuments(castToLoad, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{castToLoad.Count()} cast"); }, threads: args.threads);
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
            var upsertResult = await _sqlClient.UpsertDocuments(actors, (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{actors.Count()} actors"); }, threads: args.threads);
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        [ConsoleActionTrigger("load")]
        [ConsoleActionDescription("Load entire database. Movies, Cast, Actor. Parameters: mumber of movies to load (defaults to All), number of Threads (defaults to 8).")]
        [ConsoleActionDisplayOrder(50)]
        public async Task LoadGraph(string parameter)
        {
            await LoadMovies(parameter);
            await LoadCast(parameter);
            await LoadActors(parameter);
        }

        [ConsoleActionTrigger("gm", "getmovie")]
        [ConsoleActionDescription("Retrieve a movie by title, using the SQL API.")]
        [ConsoleActionDisplayOrder(60)]
        public async Task GetMovieByTitleSql(string parameter)
        {
            var movies = await _sqlClient.ExecuteSQL<Movie>($"select * from c where c.label = 'Movie' and c.title = '{parameter}'");

            ConsoleHelpers.ConsoleLine(movies.Result);
            ConsoleHelpers.ConsoleLine($"Success: {movies.IsSuccessful}. Execution Time: {movies.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {movies.RequestCharge} RUs");
        }

        [ConsoleActionTrigger("q", "sql")]
        [ConsoleActionDescription("Run a query")]
        [ConsoleActionDisplayOrder(200)]
        public Task ExecuteSql(string parameter)
        {
            return RunSql<dynamic>(parameter);
        }

        [ConsoleActionTrigger("q1")]
        [ConsoleActionDescription("Get all movies with a given genre (parameter)")]
        [ConsoleActionDisplayOrder(200)]
        public Task ExecuteQ1(string parameter)
        {
            var query = $"SELECT value c FROM c join g in c.Genres where g = '{parameter.ToLower()}' and c.label = 'Movie'";
            return RunSql<Movie>(query);
        }

        #region Helpers

        private static async Task RunSql<T>(string query)
        {
            var result = await _sqlClient.ExecuteSQL<T>(query);

            ConsoleHelpers.ConsoleLine($"{result.Result?.Count()} results.");
            if (result.IsSuccessful && result.Result.Any())
               ConsoleHelpers.ConsoleLine(result.Result.FirstOrDefault());
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


        /*
         * EXAMPLES:
            1. SELECT VALUE c.Title FROM c join g in c.Genres where g = 'action' and c.label = 'Movie'
         * 
        */
    }
}
