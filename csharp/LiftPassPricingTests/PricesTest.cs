using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LiftPassPricingTests
{
    public class PricesTest
    {
        [Fact]
        public async void DoesSomething()
        {
            await using var application = new WebApplicationFactory<Program>();
            
            using var client = application.CreateClient();

            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello World!", content);
        }
    }
}