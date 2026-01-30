using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NSubstitute;

namespace SUI.Find.FindApi.UnitTests.Mocks;

public abstract class MockHttpRequestData
{
    public static HttpRequestData Create(
        Dictionary<string, StringValues>? query = null,
        HttpHeadersCollection? headers = null,
        string requestData = ""
    ) => CreateJson<string>(requestData, query, headers);

    public static HttpRequestData CreateFormData(
        Dictionary<string, string> requestData,
        Dictionary<string, StringValues>? query = null,
        HttpHeadersCollection? headers = null
    )
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddFunctionsWorkerDefaults();

        var formData = new FormUrlEncodedContent(requestData);
        var bodyDataStream = new MemoryStream();
        formData.CopyToAsync(bodyDataStream).Wait();
        bodyDataStream.Position = 0;

        var context = Substitute.For<FunctionContext>();
        context.InstanceServices.Returns(serviceCollection.BuildServiceProvider());

        var queryCollection = new NameValueCollection();
        if (query != null)
        {
            foreach (var key in query.Keys)
            {
                foreach (var value in query[key])
                {
                    queryCollection.Add(key, value);
                }
            }
        }

        var request = Substitute.For<HttpRequestData>(context);
        request.Url.Returns(new Uri("http://localhost"));
        request.Body.Returns(bodyDataStream);
        request.Headers.Returns(headers ?? []);
        request.Query.Returns(queryCollection);
        request.CreateResponse().Returns(new MockHttpResponseData(context));

        return request;
    }

    public static HttpRequestData CreateJson<T>(
        T requestData,
        Dictionary<string, StringValues>? query = null,
        HttpHeadersCollection? headers = null
    )
        where T : class
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddFunctionsWorkerDefaults();

        var serializedData = JsonSerializer.Serialize(requestData);
        var bodyDataStream = new MemoryStream(Encoding.UTF8.GetBytes(serializedData));

        var context = Substitute.For<FunctionContext>();
        context.InstanceServices.Returns(serviceCollection.BuildServiceProvider());

        var queryCollection = new NameValueCollection();
        if (query != null)
        {
            foreach (var key in query.Keys)
            {
                foreach (var value in query[key])
                {
                    queryCollection.Add(key, value);
                }
            }
        }

        var request = Substitute.For<HttpRequestData>(context);
        request.Url.Returns(new Uri("http://localhost"));
        request.Body.Returns(bodyDataStream);
        request.Headers.Returns(headers ?? []);
        request.Query.Returns(queryCollection);
        request.CreateResponse().Returns(new MockHttpResponseData(context));

        return request;
    }
}
