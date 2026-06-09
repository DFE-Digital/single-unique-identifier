using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.StubCustodians.API.Controllers;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.API.Unit.Tests
{
    public class AuthControllerTests
    {
        private readonly AuthController _authController;

        public AuthControllerTests()
        {
            var logger = Substitute.For<ILogger<AuthController>>();
            _authController = new AuthController(logger);
        }

        [Fact]
        public void AuthToken_ShouldReturnProblem_WhenClientIdOrSecretIsMissing()
        {
            _authController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            };
            var request = new AuthTokenFormRequest
            {
                client_id = "",
                client_secret = "",
                grant_type = "client_credentials",
                scope = "",
            };

            var result = _authController.AuthTokenForm(request);
            Assert.NotNull(result.Result);
            Assert.IsType<ProblemHttpResult>(result.Result);
            var problemHttpResult = (ProblemHttpResult)result.Result;
            Assert.Equal(400, problemHttpResult.StatusCode);
            Assert.Equal("Invalid request", problemHttpResult.ProblemDetails.Title);
            Assert.Equal(
                "Missing client_id or client_secret.",
                problemHttpResult.ProblemDetails.Detail
            );
        }

        [Fact]
        public void AuthToken_ShouldReturnProblem_WhenClientCredentialsAreWrong()
        {
            _authController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            };
            var request = new AuthTokenRequest
            {
                ClientId = "test",
                ClientSecret = "test",
                Scopes = ["test"],
            };

            var result = _authController.AuthTokenJson(request);
            Assert.NotNull(result.Result);
            Assert.IsType<ProblemHttpResult>(result.Result);
            var problemHttpResult = (ProblemHttpResult)result.Result;
            Assert.Equal(401, problemHttpResult.StatusCode);
            Assert.Equal("Unauthorised", problemHttpResult.ProblemDetails.Title);
            Assert.Equal("Invalid client credentials.", problemHttpResult.ProblemDetails.Detail);
        }

        [Fact]
        public void AuthToken_ShouldReturnProblem_WhenScopesAreInvalid()
        {
            _authController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            };
            var request = new AuthTokenFormRequest
            {
                client_id = "SUI-SERVICE",
                client_secret = "SUIProject",
                grant_type = "client_credentials",
                scope = "test",
            };

            var result = _authController.AuthTokenForm(request);
            Assert.NotNull(result.Result);
            Assert.IsType<ProblemHttpResult>(result.Result);
            var problemHttpResult = (ProblemHttpResult)result.Result;
            Assert.Equal(401, problemHttpResult.StatusCode);
            Assert.Equal("Invalid scope", problemHttpResult.ProblemDetails.Title);
            Assert.Equal(
                "Client is not permitted to request scope(s): test.",
                problemHttpResult.ProblemDetails.Detail
            );
        }

        [Fact]
        public void AuthToken_ShouldReturnOk_WhenCredentialsAreValid()
        {
            var features = new FeatureCollection();
            var clientCredentials = Convert.ToBase64String("SUI-SERVICE:SUIProject"u8.ToArray());
            var headers = new HeaderDictionary
            {
                { "Authorization", $"Basic {clientCredentials}" },
            };
            var requestFeature = new HttpRequestFeature { Headers = headers };
            features.Set<IHeaderDictionary>(headers);
            features.Set<IHttpRequestFeature>(requestFeature);
            _authController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(features),
            };
            var request = new AuthTokenFormRequest
            {
                client_id = "SUI-SERVICE",
                client_secret = "SUIProject",
                grant_type = "client_credentials",
                scope = "find-record.read fetch-record.read",
            };

            var result = _authController.AuthTokenForm(request);
            Assert.NotNull(result.Result);
            Assert.IsType<Ok<AuthTokenResponse>>(result.Result);
            var okHttpResult = (Ok<AuthTokenResponse>)result.Result;
            Assert.Equal(200, okHttpResult.StatusCode);
            Assert.NotNull(okHttpResult.Value);
            Assert.False(string.IsNullOrEmpty(okHttpResult.Value.AccessToken));
            Assert.Equal("find-record.read fetch-record.read", okHttpResult.Value.Scope);
            Assert.Equal("Bearer", okHttpResult.Value.TokenType);
        }

        [Fact]
        public void AuthToken_ShouldReturnProblem_WhenAuthHeaderDoesNotStartWithBasic()
        {
            var features = new FeatureCollection();
            var clientCredentials = Convert.ToBase64String("SUI-SERVICE:SUIProject"u8.ToArray());
            var headers = new HeaderDictionary { { "Authorization", $"Test {clientCredentials}" } };
            var requestFeature = new HttpRequestFeature { Headers = headers };
            features.Set<IHeaderDictionary>(headers);
            features.Set<IHttpRequestFeature>(requestFeature);
            _authController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(features),
            };
            var request = new AuthTokenFormRequest
            {
                client_id = "",
                client_secret = "",
                grant_type = "client_credentials",
                scope = "find-record.read fetch-record.read",
            };

            var result = _authController.AuthTokenForm(request);
            Assert.NotNull(result.Result);
            Assert.IsType<ProblemHttpResult>(result.Result);
            var problemHttpResult = (ProblemHttpResult)result.Result;
            Assert.Equal(400, problemHttpResult.StatusCode);
            Assert.Equal("Invalid request", problemHttpResult.ProblemDetails.Title);
            Assert.Equal(
                "Missing client_id or client_secret.",
                problemHttpResult.ProblemDetails.Detail
            );
        }

        [Fact]
        public void AuthToken_ShouldReturnProblem_WhenAuthHeaderCredentialsAreWrong()
        {
            var features = new FeatureCollection();
            var clientCredentials = Convert.ToBase64String("SUI-SERVICESUIProject"u8.ToArray());
            var headers = new HeaderDictionary
            {
                { "Authorization", $"Basic {clientCredentials}" },
            };
            var requestFeature = new HttpRequestFeature { Headers = headers };
            features.Set<IHeaderDictionary>(headers);
            features.Set<IHttpRequestFeature>(requestFeature);
            _authController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(features),
            };
            var request = new AuthTokenFormRequest
            {
                client_id = "",
                client_secret = "",
                grant_type = "client_credentials",
                scope = "find-record.read fetch-record.read",
            };

            var result = _authController.AuthTokenForm(request);
            Assert.NotNull(result.Result);
            Assert.IsType<ProblemHttpResult>(result.Result);
            var problemHttpResult = (ProblemHttpResult)result.Result;
            Assert.Equal(400, problemHttpResult.StatusCode);
            Assert.Equal("Invalid request", problemHttpResult.ProblemDetails.Title);
            Assert.Equal(
                "Missing client_id or client_secret.",
                problemHttpResult.ProblemDetails.Detail
            );
        }

        [Fact]
        public void AuthToken_ShouldReturnProblem_WhenAuthHeaderIsWhiteSpace()
        {
            var features = new FeatureCollection();
            var headers = new HeaderDictionary { { "Authorization", $" " } };
            var requestFeature = new HttpRequestFeature { Headers = headers };
            features.Set<IHeaderDictionary>(headers);
            features.Set<IHttpRequestFeature>(requestFeature);
            _authController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(features),
            };
            var request = new AuthTokenFormRequest
            {
                client_id = "",
                client_secret = "",
                grant_type = "client_credentials",
                scope = "find-record.read fetch-record.read",
            };

            var result = _authController.AuthTokenForm(request);
            Assert.NotNull(result.Result);
            Assert.IsType<ProblemHttpResult>(result.Result);
            var problemHttpResult = (ProblemHttpResult)result.Result;
            Assert.Equal(400, problemHttpResult.StatusCode);
            Assert.Equal("Invalid request", problemHttpResult.ProblemDetails.Title);
            Assert.Equal(
                "Missing client_id or client_secret.",
                problemHttpResult.ProblemDetails.Detail
            );
        }
    }
}
