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

            var userId = user.FindFirstValue(ApiGatewayAuthKeys.Claims.Id.ClaimType);
            if (string.IsNullOrEmpty(userId))
                return;

            // Add the user claims to the headers
            foreach (var userClaim in ApiGatewayAuthKeys.Claims.AllUserClaims)
            {
                if (userClaim.IsMultiple)
                {
                    var claimValues = user.FindAll(userClaim.ClaimType)
                        .Select(c => c.Value)
                        .ToArray();
                    
                    if (claimValues.Length == 0)
                        continue;

                    transformContext.ProxyRequest.Headers.Add(userClaim.HeaderKey, string.Join(',', claimValues));
                }
                else
                {
                    var claimValue = user.FindFirstValue(userClaim.ClaimType);
                    if (string.IsNullOrEmpty(claimValue))
                        continue;

                    transformContext.ProxyRequest.Headers.Add(userClaim.HeaderKey, claimValue);
                }
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
