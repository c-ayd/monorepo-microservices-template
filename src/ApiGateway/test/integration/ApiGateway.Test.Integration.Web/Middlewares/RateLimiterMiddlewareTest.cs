using System.Net;
using ApiGateway.Test.Integration.Web.Collections;
using Microsoft.Net.Http.Headers;

namespace ApiGateway.Test.Integration.Web
{
    [Collection(nameof(ApiGatewayCollection))]
    public class RateLimiterMiddlewareTest
    {
        private readonly ApiGatewayCollectionCluster _collectionCluster;

        public RateLimiterMiddlewareTest(ApiGatewayCollectionCluster collectionCluster)
        {
            _collectionCluster = collectionCluster;
        }

        [Fact]
        public async Task RateLimiter_WhenNotAuthenticatedAndExceedsLimit_ShouldReturnTooManyRequests()
        {
            // Arrange
            var apiGatewayClient = _collectionCluster.ApiGatewayWebApp.CreateHttpClient();
            apiGatewayClient.DefaultRequestHeaders.Add("X-Forwarded-For", "192.168.1.1");

            // Act
            var requests = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < 30; ++i)
            {
                requests.Add(apiGatewayClient.GetAsync("/auth"));
            }

            var responses = await Task.WhenAll(requests);
            var rejectedResponse = await apiGatewayClient.GetAsync("/auth");

            // Assert
            var numberOfAccepted = 0;
            foreach (var response in responses)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    ++numberOfAccepted;
                }
            }

            Assert.Equal(requests.Count, numberOfAccepted);
            Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        }

        [Fact]
        public async Task RateLimiter_WhenAuthenticatedAndExceedsLimit_ShouldReturnTooManyRequests()
        {
            // Arrange
            var downstreamServiceClient = _collectionCluster.DownstreamServiceFixture.CreateHttpClient();
            var tokenResponse = await downstreamServiceClient.GetAsync($"/test/generate-access-token?accountId={Guid.NewGuid().ToString()}");
            var token = await tokenResponse.Content.ReadAsStringAsync();

            var apiGatewayClient = _collectionCluster.ApiGatewayWebApp.CreateHttpClient();
            apiGatewayClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, "Bearer " + token);

            // Act
            var requests = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < 100; ++i)
            {
                requests.Add(apiGatewayClient.GetAsync("/auth"));
            }

            var responses = await Task.WhenAll(requests);
            var rejectedResponse = await apiGatewayClient.GetAsync("/auth");

            // Assert
            var numberOfAccepted = 0;
            foreach (var response in responses)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    ++numberOfAccepted;
                }
            }

            Assert.Equal(requests.Count, numberOfAccepted);
            Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        }

        [Fact]
        public async Task RateLimiter_WhenAuthenticatedWithAdminRoleAndExceedsLimit_ShouldReturnTooManyRequests()
        {
            // Arrange
            var downstreamServiceClient = _collectionCluster.DownstreamServiceFixture.CreateHttpClient();
            var tokenResponse = await downstreamServiceClient.GetAsync($"/test/generate-access-token?accountId={Guid.NewGuid()}&roles=Admin");
            var token = await tokenResponse.Content.ReadAsStringAsync();

            var apiGatewayClient = _collectionCluster.ApiGatewayWebApp.CreateHttpClient();
            apiGatewayClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, "Bearer " + token);

            // Act
            var requests = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < 300; ++i)
            {
                requests.Add(apiGatewayClient.GetAsync("/auth"));
            }

            var responses = await Task.WhenAll(requests);
            var rejectedResponse = await apiGatewayClient.GetAsync("/auth");

            // Assert
            var numberOfAccepted = 0;
            foreach (var response in responses)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    ++numberOfAccepted;
                }
            }

            Assert.Equal(requests.Count, numberOfAccepted);
            Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        }
    }
}
