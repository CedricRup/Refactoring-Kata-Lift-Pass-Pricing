using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace LiftPassPricingTests
{
    public class PricesTest : IAsyncLifetime
    {
        private readonly WebApplicationFactory<Program> _application;
        private readonly HttpClient _client;

        public PricesTest()
        {
            _application = new WebApplicationFactory<Program>();
            _client = _application.CreateClient();
        }

        [Fact]
        public async void DoesSomething()
        {
            var response = await _client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello World!", content);
        }

        private async Task CreatePrice(string type, int cost)
        {
            var query = new Dictionary<string, string>();
            query.Add("type", type);
            query.Add("cost",cost.ToString());


            var url  = QueryHelpers.AddQueryString("prices", query);
            var result = await _client.PutAsync(url,null);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode); // TODO should be 204
        }

        [Fact]
        public async void DefaultCost()
        {
            Response json = await ObtainPrice("1jour");
            Assert.Equal(35, json.Cost);
        }

        [Theory]
        [InlineData(5, 0)]
        [InlineData(6, 25)]
        [InlineData(14, 25)]
        [InlineData(15, 35)]
        [InlineData(25, 35)]
        [InlineData(64, 35)]
        [InlineData(65, 27)]
        public async void WorksForAllAges(int age, int expectedCost)
        {
            Response json =  await ObtainPrice("1jour", age);
            Assert.Equal(expectedCost, json.Cost);
        }

        [Fact(Skip = "ignored")]
        public async void DefaultNightCost()
        {
            Response json = await ObtainPrice("night");
            Assert.Equal(19, json.Cost);
        }

        [Theory]
        [InlineData(5, 0)]
        [InlineData(6, 19)]
        [InlineData(25, 19)]
        [InlineData(64, 19)]
        [InlineData(65, 8)]
        public async void WorksForNightPasses(int age, int expectedCost)
        {
            Response json = await ObtainPrice("night", age);
            Assert.Equal(expectedCost, json.Cost);
        }

        [Theory]
        [InlineData(15, "2019-02-22", 35)]
        [InlineData(15, "2019-02-25", 35)]
        [InlineData(15, "2019-03-11", 23)]
        [InlineData(65, "2019-03-11", 18)]
        [InlineData(null, "2022-08-29", 23)]
        public async void WorksForMondayDeals(int? age, string date, int expectedCost)
        {
            Response json = await ObtainPrice( "1jour",  age, DateTime.Parse(date));
            Assert.Equal(expectedCost, json.Cost);
        }

        // TODO 2-4, and 5, 6 day pass

        private async Task<Response> ObtainPrice(string type, int? age = null, DateTime? date = null)
        {
            var query = new Dictionary<string, string>();
            query.Add("type", type);
            query.Add("age",age?.ToString());
            query.Add("date",date?.ToString("yyyy-MM-dd"));

            var url  = QueryHelpers.AddQueryString("prices", query);
            var result = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode); // TODO should be 204
            
            

            Assert.Equal("application/json", result.Content.Headers.ContentType.MediaType);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            return await result.Content.ReadFromJsonAsync<Response>();
        }

        public async Task InitializeAsync()
        {
            await CreatePrice("1jour", 35);
            await CreatePrice("night", 19);

        }

        public Task  DisposeAsync()
        {
            _client.Dispose();
            return _application.DisposeAsync().AsTask();
        }
    }

    class Response
    {
        public int Cost { get; set; }
    }
}