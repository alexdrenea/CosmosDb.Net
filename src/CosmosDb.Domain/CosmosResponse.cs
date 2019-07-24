using System;
using System.Net;

namespace CosmosDb.Domain
{
    /*
     Declared internal type to represent a cosmos response instead of using the types provided by the SDK (ItemResponse, Response, DatabaseResponse, etc.
     Rationale:
     1. The consumers of this library can survive breaking changes in the SDK types without breaking changes.
     2. A cosmos SDK call can end either in a Result or a CosmosException that you have to try catch. Relevant information (RequestCharge, StatucCode) 
         need to be extracted differently. This pattern never throws exception and returns all data into one response.
     3. Introduced a new property - ExecutionTimeMs that calculates time spent in Cosmoscalls. This might be useful for callers.
     4. Would have liked to add the "Headers" property from Response. However, since the SDK is one library, didn't want to pull the entire lib 
         and its dependenices in this into this otherwise lite Domain class. 
         */
    public class CosmosResponse
    {
        public bool IsSuccessful { get { return Error == null && ((int)StatusCode >= 200 && (int)StatusCode < 300); } }


        public Exception Error { get; set; }
        public string ErrorMessage { get { return Error?.Message; } }

        public HttpStatusCode StatusCode { get; set; }

        public double RequestCharge { get; set; }
        public TimeSpan ExecutionTime { get; set; }

        //public string ExecutionTimeSec { get { return $"{ExecutionTime?.TotalSeconds.ToString("#.##"} }


        public string ActivityId { get; set; }
        public string ETag { get; set; }

        /// <summary>
        /// Represents the time to wait before retrying an operation
        /// Only retuned when StatusCode is 429 (RU limit exceded)
        /// </summary>
        public TimeSpan? RetryAfter { get; set; }
        
        /// <summary>
        /// Provide a way to continue reading the FeedItemIterator from where we left off.
        /// Only returned when using ExecuteSQL
        /// </summary>
        public string ContinuationToken { get; set; }

    }

    public class CosmosResponse<T> : CosmosResponse
    {
        public T Result { get; set; }

        public CosmosResponse<U> Clone<U>()
        {
            return new CosmosResponse<U>
            {
                ActivityId = this.ActivityId,
                Error = this.Error,
                ETag = this.ETag,
                ExecutionTime = this.ExecutionTime,
                RequestCharge = this.RequestCharge,
                RetryAfter = this.RetryAfter,
                StatusCode = this.StatusCode,
            };
        }
    }
}