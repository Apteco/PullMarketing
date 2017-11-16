using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Apteco.PullMarketing.Data;
using Apteco.PullMarketing.Services;
using Microsoft.AspNetCore.TestHost;
using Moq;

namespace Apteco.PullMarketing.Api.Test.TestHelpers
{
  public static class TestServerUtilities
  {
    #region public methods
    public static Mock<IDataService> GetMockDataService(this TestServer server)
    {
      return GetTestStartup(server).MockDataService;
    }

    public static Mock<IRoutingService> GetMockRoutingService(this TestServer server)
    {
      return GetTestStartup(server).MockRoutingService;
    }

    public static async Task<HttpResponseMessage> PutJsonAsync(this HttpClient client, string requestUri, string jsonString)
    {
      return await client.PutAsync(requestUri, CreateHttpContentFromJsonString(jsonString));
    }

    public static async Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string requestUri, string jsonString)
    {
      return await client.PostAsync(requestUri, CreateHttpContentFromJsonString(jsonString));
    }

    public static async Task<HttpResponseMessage> PostFormUrlEncodedAsync(this HttpClient client, string requestUri, IEnumerable<KeyValuePair<string, string>> keysAndValues)
    {
      var content = new FormUrlEncodedContent(keysAndValues);
      return await client.PostAsync(requestUri, content);
    }
    #endregion

    #region private methods
    private static TestStartup GetTestStartup(TestServer server)
    {
      var startup = server.Host.Services.GetService(typeof(Startup)) as TestStartup;
      if (startup == null)
        throw new Exception("Couldn't get the TestStartup class from the TestServer");

      return startup;
    }

    private static HttpContent CreateHttpContentFromJsonString(string jsonString)
    {
      ByteArrayContent content = new ByteArrayContent(Encoding.UTF8.GetBytes(jsonString));
      content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
      return content;
    }
    #endregion
  }
}
