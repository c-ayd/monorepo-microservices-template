using System.Security.Claims;
using Microsoft.Net.Http.Headers;
using Shared.Http.Authentication;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace ApiGateway.Web.Transforms.Request
{
    public class JwtBearerTransform : ITransformProvider
    {
        public void Apply(TransformBuilderContext context)
        {
            context.AddRequestTransform(ApplyTransform);
        }

        private async ValueTask ApplyTransform(RequestTransformContext transformContext)
        {
            AddHeaders(transformContext);
            RemoveHeaders(transformContext);
        }

        private void AddHeaders(RequestTransformContext transformContext)
        {
            // Check if the user is authenticated
            var user = transformContext.HttpContext.User;
            if (user.Identity == null || !user.Identity.IsAuthenticated)
                return;

            var userId = user.FindFirstValue(ApiGatewayConstants.UserId.ClaimType);
            if (string.IsNullOrEmpty(userId))
                return;

            // Add the user ID to the headers
            transformContext.ProxyRequest.Headers.Add(ApiGatewayConstants.UserId.HeaderKey, userId);

            // Add the user roles to the headers
            var userRoles = user.FindAll(ApiGatewayConstants.UserRoles.ClaimType)
                .Select(c => c.Value)
                .ToArray();
            
            if (userRoles.Length > 0)
            {
                transformContext.ProxyRequest.Headers.Add(ApiGatewayConstants.UserRoles.HeaderKey, string.Join(',', userRoles));
            }

            // Add the user claims requring value checks to the headers
            foreach (var userClaim in ApiGatewayConstants.UserClaims)
            {
                var claimValue = user.FindFirstValue(userClaim.ClaimType);
                if (string.IsNullOrEmpty(claimValue))
                    continue;

                transformContext.ProxyRequest.Headers.Add(userClaim.HeaderKey, claimValue);
            }
        }

        private void RemoveHeaders(RequestTransformContext transformContext)
        {
            transformContext.ProxyRequest.Headers.Remove(HeaderNames.Authorization);
        }

        public void ValidateCluster(TransformClusterValidationContext context)
        {
        }

        public void ValidateRoute(TransformRouteValidationContext context)
        {
        }
    }
}
