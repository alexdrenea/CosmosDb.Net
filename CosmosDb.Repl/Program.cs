using CosmosDb.Domain.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb.Repl
{
    internal class Program
    {
        private CosmosDbConfig _selectedConnection;
        private Dictionary<string, CosmosDbConfig> _connections;

        private Dictionary<string, Func<string, Task>> _actions;

        private ILogger _logger = new ConsoleLogger();
        private List<JObject> _lastResultSet;

        static void Main(string[] args)
        {
            try
            {
                new Program().Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private async Task Run()
        {
            SetupActions();
            _connections = AppSettings.Instance.GetSection<CosmosDbConfig[]>("CosmosDbConfig")?.ToDictionary(d => d.Name);
            if (_connections == null || !_connections.Any())
            {
                _logger.Warning("No Connections defined. Please define a connection in appsettings.json.");
                return;
            }
            _selectedConnection = _connections.First().Value;

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Submit Gremlin:");
                Console.ResetColor();
                var queryString = Console.ReadLine();
                if (queryString == "q") break;

                var queryCommand = queryString.Split(' ').FirstOrDefault();
                if (_actions.ContainsKey(queryCommand))
                {
                    await _actions[queryCommand](queryString);
                }
                else
                {
                    if (queryString.StartsWith("gg"))
                    {
                        await GQuery(queryString.Substring(1));
                    }
                    else
                    {
                        await Query(queryString);
                    }
                }
            }
        }

        private void SetupActions()
        {
            _actions = new Dictionary<string, Func<string, Task>>
            {
                { "?", Help },
                { "help", Help },
                { "h", Help },
                { "list", ListConnections },
                { "l", ListConnections },
                { "select", SelectConnection },
                { "s", SelectConnection },
                { "csv", ExportCsv }
            };

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Type '?' or 'help' for additional commands...");
            Console.ResetColor();
        }

        #region Actions

        private async Task Help(string text)
        {
            Console.WriteLine($"Available commands:{Environment.NewLine} {string.Join($"{Environment.NewLine}  ", _actions.Keys)}");
            Console.WriteLine($"---------------");
        }

        private async Task ListConnections(string text)
        {
            Console.WriteLine($"Available connections:{Environment.NewLine} {string.Join($"{Environment.NewLine}  ", _connections.Keys)}");
            Console.WriteLine($"Selected connection:  {_selectedConnection.Name}");
            Console.WriteLine($"---------------");
        }

        private async Task SelectConnection(string text)
        {
            var name = text.Split(' ').Skip(1).FirstOrDefault();
            int index;
            if (int.TryParse(name, out index))
            {
                name = _connections.Keys.ElementAt(index);
            }

            if (name == null || !_connections.ContainsKey(name))
            {
                Console.WriteLine("Can't find connection with the given name.");
                return;
            }


            _selectedConnection = _connections[name];

            Console.WriteLine($"Selected {_selectedConnection.Name}");
        }
        private async Task GQuery(string text)
        {
            if (_selectedConnection == null)
            {
                Console.WriteLine($"No Connection selected. Use 'select _name_' to select a connection.");
                return;
            }
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                var queryResult = await _selectedConnection.GraphClient.Value.ExecuteGremlingMulti<Object>(text);
                if (!queryResult.IsSuccessful)
                {
                    Console.WriteLine($"Query failed! {queryResult.Error}");
                    return;
                }
                _lastResultSet = new List<JObject>();
                foreach (var result in queryResult.Result)
                {
                    if (!result.GetType().IsPrimitive)
                    {
                        var flat = GraphsonHelpers.GraphsonNetToFlatJObject(result);
                        _lastResultSet.Add(flat);
                        Console.WriteLine(flat.ToString());
                    }
                    Console.WriteLine(result.ToString());
                }

                sw.Stop();
                Console.WriteLine($"Total request charge: {queryResult.RU} RUs. Executed in {(queryResult.ExecutionTimeMs).ToString()}ms");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exeption running query. {Environment.NewLine}{e.Message}");
            }
        }
        private async Task Query(string text)
        {
            if (_selectedConnection == null)
            {
                Console.WriteLine($"No Connection selected. Use 'select _name_' to select a connection.");
                return;
            }
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                var queryResult = await _selectedConnection.Client.Value.ExecuteGremlingMulti<dynamic>(text);
                if (!queryResult.IsSuccessful)
                {
                    Console.WriteLine($"Query failed! {queryResult.Error}");
                    return;
                }
                _lastResultSet = new List<JObject>();
                foreach (var result in queryResult.Result)
                {
                    if (!result.GetType().IsPrimitive && result.GetType() != typeof(string))
                    {
                        _lastResultSet.Add(GraphsonHelpers.GraphsonToFlatJObject(result));
                    }
                    Console.WriteLine(result.ToString());
                }

                sw.Stop();
                Console.WriteLine($"Total request charge: {queryResult.RU} RUs. Executed in {(queryResult.ExecutionTimeMs).ToString()}ms");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exeption running query. {Environment.NewLine}{e.Message}");
            }
        }

        private async Task ExportCsv(string text)
        {
            var queryParam = text.Split(' ').Skip(1).FirstOrDefault();
            if (_lastResultSet == null || !_lastResultSet.Any())
            {
                Console.WriteLine("No result in cache. run a query first");
                return;
            }
            var fName = string.IsNullOrEmpty(queryParam) ? "default.csv" : $"{queryParam}.csv";
            var lines = new List<string>();
            lines.Add(string.Join(",", _lastResultSet.FirstOrDefault().Properties().Select(s => s.Name)));
            lines.AddRange(_lastResultSet.Select(r => string.Join(",", r.Properties().Select(p => p.Value))));
            File.WriteAllLines(fName, lines);
            Console.WriteLine($"Exported {_lastResultSet.Count} items to {fName}");
        }

        #endregion
    }
}
