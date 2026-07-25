using System.Net;

namespace AuthService.Api.Utilities
{
    public static class ResponseUtility
    {
        private const string DataKey = "data";
        private const string ErrorsKey = "errors";
        private const string MetadataKey = "metadata";

        public static IResult Success(HttpStatusCode statusCode, object? metadata = null)
            => Success((int)statusCode, metadata);
        
        public static IResult Success(int statusCode, object? metadata = null)
        {
            var response = new Dictionary<object, object?>();

            if (metadata != null) response.Add(MetadataKey, metadata);

            return Results.Json(response, statusCode: statusCode);
        }

        public static IResult SuccessPayload(HttpStatusCode statusCode, object? data = null, object? metadata = null)
            => SuccessPayload((int)statusCode, data, metadata);

        public static IResult SuccessPayload(int statusCode, object? data = null, object? metadata = null)
        {
            var response = new Dictionary<object, object?>();

            if (data != null) response.Add(DataKey, data);
            if (metadata != null) response.Add(MetadataKey, metadata);

            return Results.Json(response, statusCode: statusCode);
        }

        public static IResult Fail(HttpStatusCode statusCode, object? errors = null, object? metadata = null)
            => Fail((int)statusCode, errors, metadata);

        public static IResult Fail(int statusCode, object? errors = null, object? metadata = null)
        {
            var response = new Dictionary<object, object?>();

            if (errors != null) response.Add(ErrorsKey, errors);
            if (metadata != null) response.Add(MetadataKey, metadata);

            return Results.Json(response, statusCode: statusCode);
        }
    }
}
