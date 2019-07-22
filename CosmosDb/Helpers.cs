using CosmosDb.Domain;
using Microsoft.Azure.Cosmos;
using System;

namespace CosmosDb
{
    public static class Helpers
    {
        public static CosmosResponse<T> ToCosmosResponse<T>(this CosmosException exception)
        {
            return new CosmosResponse<T>()
            {
                Error = exception,
                StatusCode = exception.StatusCode,

                RequestCharge = exception.RequestCharge,

                ActivityId = exception.ActivityId,
                ETag = exception.Headers.ETag,

                RetryAfter = exception.RetryAfter,
            };
        }

        public static CosmosResponse ToCosmosResponse(this CosmosException exception)
        {
            return new CosmosResponse()
            {
                Error = exception,
                RequestCharge = exception.RequestCharge,

                StatusCode = exception.StatusCode,

                ActivityId = exception.ActivityId,
                ETag = exception.Headers.ETag,

                RetryAfter = exception.RetryAfter,
            };
        }

        public static CosmosResponse<T> ToCosmosResponse<T, U>(this Response<U> cosmosSDKResponse, TimeSpan? duration = null)
        {
            var res = new CosmosResponse<T>()
            {
                StatusCode = cosmosSDKResponse.StatusCode,

                RequestCharge = cosmosSDKResponse.RequestCharge,
                ExecutionTime = duration.HasValue ? duration.Value : TimeSpan.Zero,

                ActivityId = cosmosSDKResponse.ActivityId,
                ETag = cosmosSDKResponse.ETag
            };

            return res;
        }

        public static CosmosResponse ToCosmosResponse<U>(this Response<U> cosmosSDKResponse, TimeSpan? duration = null)
        {
            return new CosmosResponse()
            {
                StatusCode = cosmosSDKResponse.StatusCode,

                RequestCharge = cosmosSDKResponse.RequestCharge,
                ExecutionTime = duration.HasValue ? duration.Value : TimeSpan.Zero,

                ActivityId = cosmosSDKResponse.ActivityId,
                ETag = cosmosSDKResponse.ETag,
            };
        }
    }
}
