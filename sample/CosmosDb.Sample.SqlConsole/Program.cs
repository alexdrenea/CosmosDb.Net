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

        private IEnumerable<ConsoleAction> _actionsList;
        private Dictionary<string, Func<string, Task>> _actions;

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

                _cosmosClient = await CosmosClientSql.GetByAccountName(_accountName, _accountKey, _databaseId, _containerId);
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


        public async Task ExecuteSql(string parameter)
        {
            var movies = await _cosmosClient.ExecuteSQL<Movie>(parameter);
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
                var queryParam = queryString.Replace(queryCommand, "").Trim();
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
                new ConsoleAction(Help, "Load Movies (number of records to load)", 100, "loadmovies", "lm"),
                new ConsoleAction(Help, "Load Cast (number of cast records to load)", 2000, "loadcast", "lc"),
                new ConsoleAction(ExecuteSql, "Run a query", 1, "q", "sql")
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
