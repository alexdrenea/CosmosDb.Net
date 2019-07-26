using CosmosDb.Domain;
using CosmosDb.Sample.Shared;
using CosmosDb.Sample.Shared.Models;
using CosmosDb.Sample.Shared.Models.Graph;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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

        private IEnumerable<ConsoleAction> _actionsList;
        private Dictionary<string, Func<string, Task>> _actions;

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
                await (new Program()).RunLoop();
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


        //TODO: specific actions: get all movies: select * from c where label = '' and g.V().hasLabel('')

        public async Task LoadKeywords(string parameter = "")
        {
            //Load sample data;
            var keywords = DataLoader.LoadMovies().AllKeywords().ToList();
            //Parse parameters
            var options = parameter.Split(" ").ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), keywords.Count()) : keywords.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads

            //Upsert keywords
            var startTime = DateTime.Now;
            ConsoleLine($"Inserting {numebrOfRecords} keywords (using {threads} threads)...");
            var upsertResult = await _cosmosClient.UpsertVertex(keywords.Take(numebrOfRecords), (res) => { ConsoleLine($"processed {res.Count()}/{numebrOfRecords} keywords"); }, threads);
            PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        public async Task LoadKeywordsSquential(string parameter = "")
        {
            //Load sample data;
            var keywords = DataLoader.LoadMovies().AllKeywords().ToList();

            //Parse parameters
            var options = parameter.Split(" ").ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), keywords.Count()) : keywords.Count(); // default to all

            //Upsert keywords
            var startTime = DateTime.Now;
            var upsertResult = new List<CosmosResponse>();
            ConsoleLine($"Inserting {numebrOfRecords} keywords one at a time...");
            foreach (var k in keywords.Take(numebrOfRecords))
                upsertResult.Add(await _cosmosClient.UpsertVertex(k));
            PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        public async Task LoadMovies(string parameter = "")
        {
            //Load sample data
            var moviesCsv = DataLoader.LoadMovies();
            var movies = moviesCsv.Select(m => m.ToMovieVertex());
            var genres = moviesCsv.AllGenres().ToList();

            //Parse parameters
            var options = parameter.Split(" ").ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), movies.Count()) : movies.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads

            //Upsert movies
            var startTime = DateTime.Now;
            ConsoleLine($"Inserting {numebrOfRecords} movies (using {threads} threads)...");
            var upsertResult = await _cosmosClient.UpsertVertex(movies.Take(numebrOfRecords), (res) => { ConsoleLine($"processed {res.Count()}/{numebrOfRecords} movies"); }, threads);
            PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);

            //upsert genres - only 21, so we can just insert them
        }

        public async Task LoadActors(string parameter = "")
        {
            //Load sample data
            var castCsv = DataLoader.LoadCast();
            var actorsByMovie = castCsv.Select(c => c.ToActorVertex()).GroupBy(g => g.MovieId);

            //Parse parameters
            var options = parameter.Split(" ").ToArray();
            var numebrOfRecords = options.Length > 0 ? Math.Min(int.Parse(options[0]), actorsByMovie.Count()) : actorsByMovie.Count(); // default to all
            var threads = options.Length > 1 ? int.Parse(options[1]) : 4; //default to 4 threads

            var actorsToLoad = actorsByMovie.Take(numebrOfRecords).SelectMany(a => a);

            //Upsert movies
            var startTime = DateTime.Now;
            ConsoleLine($"Inserting {actorsToLoad.Count()} actors (using {threads} threads)...");
            var upsertResult = await _cosmosClient.UpsertVertex(actorsToLoad, (res) => { ConsoleLine($"processed {res.Count()}/{actorsToLoad.Count()} movies"); }, threads);
            PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        public async Task LoadGenres(string parameter = "")
        {
            //Load sample data
            var genres = DataLoader.LoadMovies().AllGenres().ToList();
            var vals = genres.Select(g => g.Genre);

            var startTime = DateTime.Now;

            var tasks = genres.Select(g => _cosmosClient.UpsertVertex(g));
            await Task.WhenAll(tasks);

            var upsertResult = tasks.Select(t => t.Result).ToList();
            PrintStats(upsertResult, DateTime.Now.Subtract(startTime).TotalSeconds);
        }

        public async Task LoadEdges(string parameter)
        {
            var castCsv = DataLoader.LoadCast();
            var moviesCsv = DataLoader.LoadMovies();
            var actorsByMovie = castCsv.Select(c => c.ToActorVertex());
            var movies = moviesCsv.Select(m => m.ToMovieVertex());
            var genres = moviesCsv.AllGenres().ToList();
            var keywords = moviesCsv.AllKeywords().ToList();

            //var keywordEdges = _cosmosClient.UpsertEdge(new )
        }

        public async Task GetMovieByTitleSql(string parameter)
        {
            var movies = await _cosmosClient.ExecuteSQL<MovieVertex>($"select * from c where c.label = 'Movie' and c.title = '{parameter}'");
            ConsoleLine(movies.Result);
            ConsoleLine($"Success: {movies.IsSuccessful}. Execution Time: {movies.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {movies.RequestCharge} RUs");
        }

        public async Task GetMovieByTitleGraph(string parameter)
        {
            var movies = await _cosmosClient.ExecuteGremlinSingle<MovieVertex>($"g.V().hasLabel('Movie').has('Title','{parameter}'");
            ConsoleLine(movies.Result);
            ConsoleLine($"Success: {movies.IsSuccessful}. Execution Time: {movies.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {movies.RequestCharge} RUs");
        }


        public async Task ExecuteSql(string parameter)
        {
            var movies = await _cosmosClient.ExecuteSQL<object>(parameter);
            ConsoleLine($"{movies.Result?.Count()} results.");
            ConsoleLine($"Success: {movies.IsSuccessful}. Execution Time: {movies.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {movies.RequestCharge} RUs");
        }

        public async Task ExecuteGremlin(string parameter)
        {
            var movies = await _cosmosClient.ExecuteGremlin<object>(parameter);
            ConsoleLine($"{movies.Result?.Count()} results.");
            ConsoleLine($"Success: {movies.IsSuccessful}. Execution Time: {movies.ExecutionTime.TotalSeconds.ToString("#.##")} s. Execution cost: {movies.RequestCharge} RUs");
        }

        #region REPL Setup

        private async Task RunLoop()
        {
            SetupActions();
            while (true)
            {
                ConsoleInLine(":>", ConsoleColor.DarkYellow);
                var queryString = Console.ReadLine();
                if (queryString == "q") break;

                var queryCommand = queryString.Split(' ').FirstOrDefault();
                var queryParam = queryString.Replace($"{queryCommand} ", "").Trim();
                if (_actions.ContainsKey(queryCommand))
                {
                    try
                    {
                        await _actions[queryCommand](queryParam);
                    }
                    catch (Exception e)
                    {
                        ConsoleInLine($"Error:", ConsoleColor.DarkRed);
                        ConsoleLine(e.Message);
                    }
                }
                else
                {
                    ConsoleLine($":> Unrecognized command '{queryCommand}'. Use ? or help for a list of available commands", ConsoleColor.DarkRed);
                }
            }
        }

        private void SetupActions()
        {
            _actionsList = new ConsoleAction[]
            {
                new ConsoleAction(Help, "Displays this message", 0, "h", "help", "?"),
                new ConsoleAction(LoadMovies, "Load Movies bulk. Parameters: number of movies to load (defaults to All), number of Threads (defaults to 4)", 100, "lm", "loadmovies"),
                new ConsoleAction(LoadActors, "Load Actors bulk. Parameters: number of movies to load actors for (defaults to All), number of threads (defaults to 4)", 100, "la", "loadactors"),
                new ConsoleAction(LoadKeywords, "Load Keywords bulk. Parameters: number of keywords to load (defaults to All), number of threads (defaults to 4). Demonstrates use of Bulk loading capabilities", 100, "lk", "loadkeywords"),
                new ConsoleAction(LoadGenres, "Load Genres", 100, "lg", "loadgenres"),
                new ConsoleAction(LoadKeywordsSquential, "Load Keywords sequentially (await each insert). Optional parameters number of records.", 100, "lks", "loadkeywordseq"),
                

                new ConsoleAction(Help, "Load Cast (number of cast records to load)", 2000, "lc", "loadcast"),
                new ConsoleAction(ExecuteSql, "Run a query", 1, "q", "sql"),
                new ConsoleAction(ExecuteGremlin, "Run a gremlin traversal", 1, "g", "gremlin"),
                new ConsoleAction(GetMovieByTitleSql, "Retrieve a movie by title, using the SQL API", 2, "s.gm", "s.getmovie"),
                new ConsoleAction(GetMovieByTitleGraph, "Retrieve a movie by title, using the Gremlin", 2, "g.gm", "g.getmovie"),
            };

            _actions = _actionsList.SelectMany(ca => ca.Commands.Select(c => new KeyValuePair<string, Func<string, Task>>(c, ca.Action)))
                                   .ToDictionary(k => k.Key, v => v.Value);


            ConsoleLine("Type '?' or 'help' for additional commands...", ConsoleColor.DarkGray);
        }

        private async Task Help(string text)
        {
            var actions = _actionsList.OrderBy(o => o.Order).ToDictionary(k => string.Join(", ", k.Commands), v => v.Description);
            var maxCommand = actions.Max(a => a.Key.Length);

            Console.WriteLine();
            Console.WriteLine($"Available commands:");
            foreach (var a in actions)
                Console.WriteLine($"{a.Key.PadLeft(maxCommand)} : {a.Value}");
            Console.WriteLine($"{"q".PadLeft(maxCommand)} : Exit");
            Console.WriteLine();
        }

        private static void ConsoleLine<T>(T entity, ConsoleColor? color = null)
        {
            if (color.HasValue)
                Console.ForegroundColor = color.Value;
            Console.WriteLine(entity != null ? JsonConvert.SerializeObject(entity) : "null");
            Console.ResetColor();
        }

        private static void ConsoleLine(string message, ConsoleColor? color = null)
        {
            if (color.HasValue)
                Console.ForegroundColor = color.Value;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void ConsoleInLine(string message, ConsoleColor? color = null)
        {
            if (color.HasValue)
                Console.ForegroundColor = color.Value;
            Console.Write(message.Trim() + " "); //make sure there's a space at the end of the message since it's inline.
            Console.ResetColor();
        }

        internal struct ConsoleAction
        {
            public ConsoleAction(Func<string, Task> action, string description, params string[] commands)
                : this(action, description, int.MaxValue, commands)
            {
            }

            public ConsoleAction(Func<string, Task> action, string description, int order, params string[] commands)
            {
                Action = action;
                Description = description;
                Commands = commands;
                Order = order;
            }

            /// <summary>
            /// Gets or sets the list of commands that will trigger the action
            /// </summary>
            public IEnumerable<string> Commands { get; set; }

            /// <summary>
            /// Shows up as the description of the command when run help
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Action to be executed when a the command is called.
            /// The function gets a string that represents whatever the user entered after the command
            /// It is the method's responsability to parse that input into whatever it needs.
            /// </summary>
            public Func<string, Task> Action { get; set; }
            public int Order { get; set; }
        }

        #endregion

        #region Helpers

        private static void PrintStats(IEnumerable<CosmosResponse> results, double executionTimeSeconds)
        {
            var totalCosmosTime = results.Sum(u => u.ExecutionTime.TotalSeconds);
            var totalRequestCharge = results.Sum(u => u.RequestCharge);

            ConsoleLine($"Inserted {results.Count()} entities. {results.Count(k => !k.IsSuccessful)} failed. " +
                    $"Total Request Charge: {totalRequestCharge.ToString("#.##")} Request Units. ({(totalRequestCharge / results.Count()).ToString("#.##")} RU/entity, {(totalRequestCharge / executionTimeSeconds).ToString("#.##")} RU/sec). " +
                    $"Execution time: {executionTimeSeconds.ToString("#.##")} sec. Cosmos execution time: {totalCosmosTime.ToString("#.##")} sec");

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
