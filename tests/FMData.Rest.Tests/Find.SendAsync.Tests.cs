using FMData.Rest.Requests;
using FMData.Rest.Tests.TestModels;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace FMData.Rest.Tests
{
    public class FindRequestTests
    {
        private static readonly string server = "http://localhost";
        private static readonly string file = "test-file";
        private static readonly string user = "unit";
        private static readonly string pass = "test";

        private static IFileMakerApiClient GetMockedFDC()
        {
            var mockHttp = new MockHttpMessageHandler();

            

            mockHttp.When(HttpMethod.Post, $"{server}/fmi/data/v1/databases/{file}/sessions")
               .Respond("application/json", DataApiResponses.SuccessfulAuthentication());

            mockHttp.When(HttpMethod.Get, $"{server}/fmi/data/v1/databases/{file}/layout*")
                .Respond("application/json", DataApiResponses.SuccessfulFind());

            mockHttp.When(HttpMethod.Post, $"{server}/fmi/data/v1/databases/{file}/layouts/layout/_find")
                .Respond("application/json", DataApiResponses.SuccessfulFind());

            mockHttp.When(HttpMethod.Post, $"{server}/fmi/data/v1/databases/{file}/layouts/Users/_find")
                .Respond("application/json", DataApiResponses.SuccessfulFind());

            var fdc = new FileMakerRestClient(mockHttp.ToHttpClient(), server, file, user, pass);
            return fdc;
        }

        private IFindRequest<Dictionary<string, string>> FindReq => new FindRequest<Dictionary<string, string>>()
        {
            Query = new List<Dictionary<string, string>>()
            {
                new Dictionary<string,string>()
                {
                    {"Name","fuzzzerd"}
                },
                new Dictionary<string,string>()
                {
                    {"Name","Admin"}, {"omit","true"},
                }
            },
            Layout = "layout"
        };

        private IFindRequest<User> FindUserReqWithLayoutOverride => new FindRequest<User>()
        {
            Query = new List<User>()
            {
                new User()
                {
                    Id =1
                }
            },
            Layout = "layout"
        };

        [Fact]
        public async Task SendAsync_Find_Should_ReturnData()
        {
            var fdc = GetMockedFDC();

            var response = await fdc.SendAsync(FindReq);

            var responseDataContainsResult = response.Response.Data.Any(r => r.FieldData.Any(v => v.Value.Contains("Buzz")));

            Assert.True(responseDataContainsResult);
        }

        [Fact]
        public async Task SendAsync_EmptyFind_ShouldReturnMany()
        {
            var fdc = GetMockedFDC();

            var response = await fdc.SendAsync(new FindRequest<User> { Layout = "layout" });

            Assert.Equal(2, response.Count());
        }

        [Fact]
        public async Task SendAsync_FindWithoutQuery_ShouldConvertToGetRange_AndReturnMany()
        {
            var fdc = GetMockedFDC();

            var response = await fdc.SendAsync(new FindRequest<Dictionary<string, string>>() { Layout = "layout" });

            Assert.Equal(2, response.Response.Data.Count());
        }

        [Fact]
        public async Task SendAsync_FindWithoutlayout_ShouldThrowArgumentException()
        {
            var fdc = GetMockedFDC();

            await Assert.ThrowsAsync<ArgumentException>(async () => await fdc.SendAsync(new FindRequest<Dictionary<string, string>>() { }));
        }

        [Fact]
        public async Task SendAsync_StrongType_FindShould_ReturnData()
        {
            // arrange
            var fdc = GetMockedFDC();

            // act
            var response = await fdc.SendAsync(FindUserReqWithLayoutOverride);

            // assert
            var responseDataContainsResult = response.Any(r => r.Name.Contains("Buzz"));
            Assert.True(responseDataContainsResult);
        }

        [Fact]
        public async Task SendAsync_StrongType_FindShould_ReturnData_AndGetFMID()
        {
            // arrange
            var fdc = GetMockedFDC();

            // act
            var response = await fdc.SendAsync(FindUserReqWithLayoutOverride, (u, i) => u.FileMakerRecordId = i);

            // assert
            var responseDataContainsResult = response.All(r => r.FileMakerRecordId != 0);
            Assert.True(responseDataContainsResult);
        }

        [Fact]
        public async Task SendAsync_StrongType_FindShould_ReturnData_AndGetModID()
        {
            // arrange
            var fdc = GetMockedFDC();

            // act
            var response = await fdc.SendAsync(FindUserReqWithLayoutOverride, null, (u, i) => u.FileMakerModId = i);

            // assert (any becuase our data is mixed and has both)
            var responseDataContainsResult = response.Any(r => r.FileMakerModId != 0);
            Assert.True(responseDataContainsResult);
        }

        [Fact]
        public async Task SendAsyncFind_WithBadLayout_Throws()
        {
            // arrange
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When(HttpMethod.Post, $"{server}/fmi/data/v1/databases/{file}/sessions")
                           .Respond("application/json", DataApiResponses.SuccessfulAuthentication());

            mockHttp.When(HttpMethod.Post, $"{server}/fmi/data/v1/databases/{file}/layouts/*")
                .Respond(HttpStatusCode.InternalServerError, "application/json", DataApiResponses.LayoutNotFound());

            var fdc = new FileMakerRestClient(mockHttp.ToHttpClient(), server, file, user, pass);

            // act
            // assert
            await Assert.ThrowsAsync<Exception>(async () => await fdc.SendAsync(FindUserReqWithLayoutOverride));
        }

        [Fact]
        public async Task SendAsyncFind_Record_ThatDoesNotExist_ShouldReturnEmpty()
        {
            // arrange
            var mockHttp = new MockHttpMessageHandler();

            var layout = FileMakerRestClient.GetTableName(new User());

            mockHttp.When(HttpMethod.Post, $"{server}/fmi/data/v1/databases/{file}/sessions")
                           .Respond("application/json", DataApiResponses.SuccessfulAuthentication());

            mockHttp.When(HttpMethod.Post, $"{server}/fmi/data/v1/databases/{file}/layouts/{layout}/_find")
                .Respond(HttpStatusCode.InternalServerError, "application/json", DataApiResponses.FindNotFound());

            var fdc = new FileMakerRestClient(mockHttp.ToHttpClient(), server, file, user, pass);

            // act
            var toFind = new User() { Id = 35 };
            var response = await fdc.SendAsync(new FindRequest<User>() { Query = new List<User> { toFind }, Layout = layout });

            // assert
            Assert.Empty(response);
        }

        [Fact]
        public async Task SendAsyncFind_WithoutLayout_ShouldThrow()
        {
            // arrange
            var mockHttp = new MockHttpMessageHandler();

            var layout = FileMakerRestClient.GetTableName(new User());

            mockHttp.When(HttpMethod.Post, $"{server}/fmi/data/v1/databases/{file}/sessions")
                           .Respond("application/json", DataApiResponses.SuccessfulAuthentication());

            mockHttp.When(HttpMethod.Post, $"{server}/fmi/data/v1/databases/{file}/layouts/{layout}/_find")
                .Respond(HttpStatusCode.InternalServerError, "application/json", DataApiResponses.FindNotFound());

            var fdc = new FileMakerRestClient(mockHttp.ToHttpClient(), server, file, user, pass);

            // act
            var toFind = new User() { Id = 35 };

            // assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await fdc.SendAsync(new FindRequest<User>() { Query = new List<User> { toFind } }));
        }

        [Fact]
        public async Task SendAsync_Dictionary_WithPortals_ShouldHaveData()
        {
            // arrange
            var mockHttp = new MockHttpMessageHandler();

            var layout = "the-layout";

            mockHttp.When(HttpMethod.Post, $"{server}/fmi/data/v1/databases/{file}/sessions")
                           .Respond("application/json", DataApiResponses.SuccessfulAuthentication());

            mockHttp.When(HttpMethod.Post, $"{server}/fmi/data/v1/databases/{file}/layouts/{layout}/_find")
                .Respond(HttpStatusCode.OK, "application/json", DataApiResponses.SuccessfulFindWithPortal());

            var fdc = new FileMakerRestClient(mockHttp.ToHttpClient(), server, file, user, pass);

            var fr = new FindRequest<Dictionary<string, string>>
            {
                Layout = layout,
                Query = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { { "one", "one" } }
                }
            };

            // act
            var response = await fdc.SendAsync(fr);

            // assert
            Assert.NotEmpty(response.Response.Data);
        }
    }
}