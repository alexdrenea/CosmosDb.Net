using CosmosDb.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb
{
    public class CosmosResponse : CosmosResponse<object>
    {

    }

    public class CosmosResponse<T>
    {
        public Exception Error { get; set; }

        public bool IsSuccessful { get { return Error == null; } }

        public T Result { get; set; }
        public double RU { get; set; }
    }
}