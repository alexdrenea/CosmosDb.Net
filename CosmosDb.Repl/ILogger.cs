using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb.Repl
{
    public enum LogLevel
    {
        All,
        Info,
        Warning,
        Error
    }
    public interface ILogger
    {
        void Verbose(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception ex);
    }
}
