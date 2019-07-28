using CosmosDB.Net.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb.Sample.Shared
{
    public class ConsoleHelpers
    {
        public static void ConsoleLine<T>(T entity, ConsoleColor? color = null)
        {
            if (color.HasValue)
                Console.ForegroundColor = color.Value;
            Console.WriteLine(entity != null ? JsonConvert.SerializeObject(entity, Formatting.Indented) : "null");
            Console.ResetColor();
        }

        public static void ConsoleLine(string message, ConsoleColor? color = null)
        {
            if (color.HasValue)
                Console.ForegroundColor = color.Value;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void ConsoleInLine(string message, ConsoleColor? color = null)
        {
            if (color.HasValue)
                Console.ForegroundColor = color.Value;
            Console.Write(message.Trim() + " "); //make sure there's a space at the end of the message since it's inline.
            Console.ResetColor();
        }

        public static void PrintStats(IEnumerable<CosmosResponse> results, double executionTimeSeconds)
        {
            var totalCosmosTime = results.Sum(u => u.ExecutionTime.TotalSeconds);
            var totalRequestCharge = results.Sum(u => u.RequestCharge);

            ConsoleLine($"Inserted {results.Count()} entities. {results.Count(k => !k.IsSuccessful)} failed. " +
                    $"Total Request Charge: {totalRequestCharge.ToString("#.##")} Request Units. ({(totalRequestCharge / results.Count()).ToString("#.##")} RU/entity, {(totalRequestCharge / executionTimeSeconds).ToString("#.##")} RU/sec). " +
                    $"Execution time: {executionTimeSeconds.ToString("#.##")} sec. Cosmos execution time: {totalCosmosTime.ToString("#.##")} sec");

        }
    }

    public class ConsoleREPL
    {
        private object _executionContext;
        private List<ConsoleAction> _actionsList;
        private Dictionary<string, ConsoleAction> _actions;

        public ConsoleREPL(object executionContext)
        {
            _executionContext = executionContext;
        }

        public string Prompt { get; set; } = ":>";

        public async Task RunLoop()
        {
            SetupActions();
            while (true)
            {
                Console.WriteLine();
                ConsoleHelpers.ConsoleInLine(Prompt, ConsoleColor.DarkYellow);
                var queryString = Console.ReadLine();
                if (queryString == "q") break;

                var queryCommand = queryString.Split(' ').FirstOrDefault();
                var queryParam = queryString.Replace($"{queryCommand} ", "").Trim();
                if (_actions.ContainsKey(queryCommand))
                {
                    try
                    {
                        await _actions[queryCommand].ExecuteAction(queryParam);
                    }
                    catch (Exception e)
                    {
                        ConsoleHelpers.ConsoleInLine($"Error:", ConsoleColor.DarkRed);
                        ConsoleHelpers.ConsoleLine(e.Message);
                    }
                }
                else
                {
                    ConsoleHelpers.ConsoleLine($"{Prompt} Unrecognized command '{queryCommand}'. Use ? or help for a list of available commands", ConsoleColor.DarkRed);
                }
            }
        }

        public void SetupActions()
        {
            var allMethods = _executionContext.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            _actionsList = allMethods.Where(m => m.GetCustomAttribute<ConsoleActionTriggerAttribute>() != null).Select(m => new ConsoleAction(m, _executionContext)).ToList();
            _actionsList.Insert(0, new ConsoleAction(Help, "Displays this message", 0, "h", "help", "?"));
            
            _actions = _actionsList.SelectMany(ca => ca.Commands.Select(c => new KeyValuePair<string, ConsoleAction>(c, ca)))
                               .ToDictionary(k => k.Key, v => v.Value);

            ConsoleHelpers.ConsoleLine("Type '?' or 'help' for additional commands...", ConsoleColor.DarkGray);
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
    }

    public struct ConsoleAction
    {
        public ConsoleAction(Func<string, Task> action, string description, params string[] commands)
            : this(action, description, int.MaxValue, commands)
        {
        }

        public ConsoleAction(Func<string, Task> action, string description, int order, params string[] commands)
        {
            ActionFunc = action;
            Action = null;
            ActionContext = null;
            Commands = commands;
            Description = description;
            Order = order;
        }

        public ConsoleAction(MethodInfo action, object actionContext)
        {
            ActionFunc = null;
            Action = action;
            ActionContext = actionContext;
            Commands = action.GetCustomAttribute<ConsoleActionTriggerAttribute>().Triggers;
            Description = action.GetCustomAttribute<ConsoleActionDescriptionAttribute>()?.Description ?? "";
            Order = action.GetCustomAttribute<ConsoleActionDisplayOrderAttribute>()?.Order ?? int.MaxValue;
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
        public Func<string, Task> ActionFunc { get; set; }

        public MethodInfo Action { get; set; }
        public object ActionContext { get; set; }
        public int Order { get; set; }



        public Task ExecuteAction(string parameter)
        {
            if (ActionFunc != null)
            {
                return ActionFunc(parameter);
            }
            if (Action != null && ActionContext != null)
            {
                var res = Action.Invoke(ActionContext, new[] { parameter });
                return (res is Task) ? (Task)res : Task.FromResult(res);
            }

            throw new InvalidOperationException("Either ActionFunc or Action must be defined");
        }
    }

    public class ConsoleActionTriggerAttribute : Attribute
    {
        public ConsoleActionTriggerAttribute(params string[] triggers)
        {
            Triggers = triggers;
        }
        public IEnumerable<string> Triggers { get; set; }
    }

    public class ConsoleActionDescriptionAttribute : Attribute
    {
        public ConsoleActionDescriptionAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; set; }
    }

    public class ConsoleActionDisplayOrderAttribute : Attribute
    {
        public ConsoleActionDisplayOrderAttribute(int order)
        {
            Order = order;
        }

        public int Order { get; set; }
    }
}
