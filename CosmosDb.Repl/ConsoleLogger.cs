using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb.Repl
{
    public class ConsoleLogger : ILogger
    {
        private readonly LogLevel _level;
        public ConsoleLogger(LogLevel level = LogLevel.All)
        {
            _level = level;
        }

        public void Error(string message, Exception ex)
        {
            Console.WriteLine($"ERROR: {message}{Environment.NewLine}{ex.Message}{Environment.NewLine}-----");
        }

        public void Info(string message)
        {
            if (_level <= LogLevel.Info)
                Console.WriteLine($"INFO: {message}");
        }

        public void Verbose(string message)
        {
            if (_level <= LogLevel.All)
                Console.WriteLine($"VERBOSE: {message}");
        }

        public void Warning(string message)
        {
            if (_level <= LogLevel.Warning)
                Console.WriteLine($"Warning: {message}");
        }
    }
}
