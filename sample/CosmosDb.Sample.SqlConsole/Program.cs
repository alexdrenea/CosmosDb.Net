using CosmosDb.Sample.Shared;
using CosmosDb.Sample.Shared.Models;
using CosmosDb.Sample.Shared.Models.Sql;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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
        private static ICosmosClientSql _cosmosClient;

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
        [ConsoleActionDescription("Load Movies bulk. Parameters: number of movies to load (defaults to All), number of Threads (defaults to 4).")]
        [ConsoleActionDisplayOrder(10)]
        public async Task LoadMovies(string parameter = "")
        {
            //Load sample data
            var moviesCsv = DataLoader.LoadMovies();
            var movies = moviesCsv.Select(m => m.ToMovie());

            //Parse parameters
            var options = parameter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), movies.Count()) : movies.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads

            //Upsert movies
            var startTime = DateTime.Now;
            ConsoleHelpers.ConsoleLine($"Inserting {numebrOfRecords} movies (using {threads} threads)...");
            var upsertResult = await _cosmosClient.UpsertDocuments(movies.Take(numebrOfRecords), (res) => { ConsoleHelpers.ConsoleLine($"processed {res.Count()}/{numebrOfRecords} movies"); }, threads);
            ConsoleHelpers.PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }


        [ConsoleActionTrigger("gm", "getmovie")]
        [ConsoleActionDescription("Retrieve a movie by title, using the SQL API.")]
        [ConsoleActionDisplayOrder(60)]
        public async Task GetMovieByTitleSql(string parameter)
        {
            var movies = await _cosmosClient.ExecuteSQL<Movie>($"select * from c where c.label = 'Movie' and c.title = '{parameter}'");

            ConsoleHelpers.ConsoleLine(movies.Result);
            ConsoleHelpers.ConsoleLine($"Success: {movies.IsSuccessful}. Execution Time: {movies.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {movies.RequestCharge} RUs");
        }

        [ConsoleActionTrigger("q", "sql")]
        [ConsoleActionDescription("Run a query")]
        [ConsoleActionDisplayOrder(200)]
        public async Task ExecuteSql(string parameter)
        {
            var result = await _cosmosClient.ExecuteSQL<dynamic>(parameter);

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
