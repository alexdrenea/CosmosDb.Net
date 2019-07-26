using System;
using System.Collections.Generic;
using System.Text;

namespace CosmosDb.Sample.GraphConsole
{
    //Create an attribute based loaded for method where you can describe it via an attribute and have the code load it automatically
    public class CommandAttribute
    {
        public string[] Keywords { get; set; }
        public string Description { get; set; }
    }
    public class ReplHelpers
    {

    }
}
