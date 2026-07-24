using System.Net;
using AuthService.Api.Utilities;
using AuthService.Application.Validations;

namespace AuthService.Api.Filters
{
    public static class ValidationFilter
    {
        private const string _requestBodyMissingCode = "auth_error_req_body_missing";
        private static readonly ValidationError _requestBodyMissingError = new ValidationError(
            Message: "The request body is missing",
            Code: _requestBodyMissingCode
        );

        /// <summary>
        /// Adds a validation filter to the endpoint.
        /// </summary>
        /// <param name="validator">Validator to run against the request</param>
        public static IEndpointConventionBuilder AddValidation<T>(this IEndpointConventionBuilder builder,
            IValidator<T> validator)
        {
            return builder.AddEndpointFilter(async (context, next) =>
            {
                var request = context.Arguments.OfType<T>().FirstOrDefault();
                if (request == null)
                    return ResponseUtility.Fail(HttpStatusCode.BadRequest, new[] { _requestBodyMissingError });

                var errors = validator.Validate(request);

                if (errors.Count > 0)
                    return ResponseUtility.Fail(HttpStatusCode.BadRequest, errors);

                return await next(context);
            });
        }

        /// <summary>
        /// Adds validation filters to the endpoint.
        /// </summary>
        /// <param name="validators">Validators to run against the request</param>
        /// <param name="stopOnFail">Whether to stop the validation if the first error occurs</param>
        public static IEndpointConventionBuilder AddValidations<T>(this IEndpointConventionBuilder builder,
            IEnumerable<IValidator<T>> validators,
            bool stopOnFail = true)
        {
            return builder.AddEndpointFilter(async (context, next) =>
            {
                var request = context.Arguments.OfType<T>().FirstOrDefault();
                if (request == null)
                    return ResponseUtility.Fail(HttpStatusCode.BadRequest, new[] { _requestBodyMissingError });

                var errors = new List<ValidationError>();
                foreach (var validator in validators)
                {
                    errors.AddRange(validator.Validate(request));
                    if (stopOnFail && errors.Count > 0)
                        return ResponseUtility.Fail(HttpStatusCode.BadRequest, errors);
                }

                if (errors.Count > 0)
                    return ResponseUtility.Fail(HttpStatusCode.BadRequest, errors);

                return await next(context);
            });
        }

        /// <summary>
        /// Adds an async validator to the endpoint.
        /// </summary>
        /// <param name="validator">Async validator to run against the request</param>
        public static IEndpointConventionBuilder AddAsyncValidation<T>(this IEndpointConventionBuilder builder,
            IValidatorAsync<T> validator)
        {
            return builder.AddEndpointFilter(async (context, next) =>
            {
                var request = context.Arguments.OfType<T>().FirstOrDefault();
                if (request == null)
                    return ResponseUtility.Fail(HttpStatusCode.BadRequest, new[] { _requestBodyMissingError });

                var errors = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
                if (errors.Count > 0)
                    return ResponseUtility.Fail(HttpStatusCode.BadRequest, errors);

                return await next(context);
            });
        }

        /// <summary>
        /// Adds async validators to the endpoints.
        /// </summary>
        /// <param name="validators">Async validator to run against the request</param>
        /// <param name="stopOnFail">Whether to stop the validation if the first error occurs</param>
        public static IEndpointConventionBuilder AddAsyncValidations<T>(this IEndpointConventionBuilder builder,
            IEnumerable<IValidatorAsync<T>> validators,
            bool stopOnFail = true)
        {
            return builder.AddEndpointFilter(async (context, next) =>
            {
                var request = context.Arguments.OfType<T>().FirstOrDefault();
                if (request == null)
                    return ResponseUtility.Fail(HttpStatusCode.BadRequest, new[] { _requestBodyMissingError });

                var errors = new List<ValidationError>();
                foreach (var validator in validators)
                {
                    errors.AddRange(await validator.ValidateAsync(request));
                    if (stopOnFail && errors.Count > 0)
                        return ResponseUtility.Fail(HttpStatusCode.BadRequest, errors);
                }

                if (errors.Count > 0)
                    return ResponseUtility.Fail(HttpStatusCode.BadRequest, errors);

                return await next(context);
            });
        }
    }
}
