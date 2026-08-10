using System.Net;
using Common.Http.Exceptions;
using Common.Http.Response;
using Common.Http.Response.Structures;
using Common.Http.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Http.DependencyInjection
{
    public static partial class DependencyInjection
    {
        private static readonly ErrorItem _requestBodyMissingError = new ErrorItem(
            Code: "req_body_missing",
            Message: "The request body is missing"
        );

        /// <summary>
        /// Adds a validation filter to the endpoint.
        /// </summary>
        /// <param name="validator">Validator to run against the request</param>
        public static IEndpointConventionBuilder AddValidation<T>(this IEndpointConventionBuilder builder)
        {
            return builder.AddEndpointFilter(async (context, next) =>
            {
                var request = context.Arguments.OfType<T>().FirstOrDefault();
                if (request == null)
                    return JsonResponseBuilder.Error(HttpStatusCode.BadRequest, [_requestBodyMissingError]);

                var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
                if (validator == null)
                    throw new ValidatorNotFoundException($"{nameof(IValidator<>)}<{typeof(T).Name}>");

                var errors = validator.Validate(request);
                if (errors.Count > 0)
                    return JsonResponseBuilder.Error(HttpStatusCode.BadRequest, errors);

                return await next(context);
            });
        }

        /// <summary>
        /// Adds validation filters to the endpoint.
        /// </summary>
        /// <param name="validators">Validators to run against the request</param>
        /// <param name="stopOnFirstFail">Whether to stop the validation if a validator returns errors</param>
        public static IEndpointConventionBuilder AddValidations<T>(this IEndpointConventionBuilder builder,
            IEnumerable<IValidator<T>> validators,
            bool stopOnFirstFail = true)
        {
            return builder.AddEndpointFilter(async (context, next) =>
            {
                var request = context.Arguments.OfType<T>().FirstOrDefault();
                if (request == null)
                    return JsonResponseBuilder.Error(HttpStatusCode.BadRequest, [_requestBodyMissingError]);

                var errors = new List<ErrorItem>();
                foreach (var validator in validators)
                {
                    errors.AddRange(validator.Validate(request));
                    if (stopOnFirstFail && errors.Count > 0)
                        return JsonResponseBuilder.Error(HttpStatusCode.BadRequest, errors);
                }

                if (errors.Count > 0)
                    return JsonResponseBuilder.Error(HttpStatusCode.BadRequest, errors);

                return await next(context);
            });
        }

        /// <summary>
        /// Adds an async validator to the endpoint.
        /// </summary>
        /// <param name="validator">Async validator to run against the request</param>
        public static IEndpointConventionBuilder AddAsyncValidation<T>(this IEndpointConventionBuilder builder)
        {
            return builder.AddEndpointFilter(async (context, next) =>
            {
                var request = context.Arguments.OfType<T>().FirstOrDefault();
                if (request == null)
                    return JsonResponseBuilder.Error(HttpStatusCode.BadRequest, [_requestBodyMissingError]);

                var validator = context.HttpContext.RequestServices.GetRequiredService<IAsyncValidator<T>>();
                if (validator == null)
                    throw new AsyncValidatorNotFoundException($"{nameof(IAsyncValidator<>)}<{typeof(T).Name}>");

                var errors = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
                if (errors.Count > 0)
                    return JsonResponseBuilder.Error(HttpStatusCode.BadRequest, errors);

                return await next(context);
            });
        }

        /// <summary>
        /// Adds async validators to the endpoints.
        /// </summary>
        /// <param name="validators">Async validator to run against the request</param>
        /// <param name="stopOnFirstFail">Whether to stop the validation if a validator returns errors</param>
        public static IEndpointConventionBuilder AddAsyncValidations<T>(this IEndpointConventionBuilder builder,
            IEnumerable<IAsyncValidator<T>> validators,
            bool stopOnFirstFail = true)
        {
            return builder.AddEndpointFilter(async (context, next) =>
            {
                var request = context.Arguments.OfType<T>().FirstOrDefault();
                if (request == null)
                    return JsonResponseBuilder.Error(HttpStatusCode.BadRequest, [_requestBodyMissingError]);

                var errors = new List<ErrorItem>();
                foreach (var validator in validators)
                {
                    errors.AddRange(await validator.ValidateAsync(request));
                    if (stopOnFirstFail && errors.Count > 0)
                        return JsonResponseBuilder.Error(HttpStatusCode.BadRequest, errors);
                }

                if (errors.Count > 0)
                    return JsonResponseBuilder.Error(HttpStatusCode.BadRequest, errors);

                return await next(context);
            });
        }
    }
}
