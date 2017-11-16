using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Apteco.PullMarketing.Api.Test.TestHelpers
{
  public static class TestUtilities
  {
    public static void AssertResponseHasStatusCodeAndBody(HttpStatusCode expectedStatusCode, string expectedBody, HttpResponseMessage response)
    {
      string messageBody = null;
      if (response != null)
      {
        Task<string> readBodyTask = response.Content.ReadAsStringAsync();
        readBodyTask.Wait();
        messageBody = readBodyTask.Result;
      }

      Assert.IsNotNull(response, "Response was null");
      Assert.AreEqual(expectedStatusCode, response.StatusCode, "Status code didn't meet expectation");
      Assert.AreEqual(expectedBody, messageBody, "Body didn't meet expectation");
    }

    public static void AssertResponseHasStatusCodeAndBodyStartsWith(HttpStatusCode expectedStatusCode, string expectedBodyPrefix, HttpResponseMessage response)
    {
      string messageBody = null;
      if (response != null)
      {
        Task<string> readBodyTask = response.Content.ReadAsStringAsync();
        readBodyTask.Wait();
        messageBody = readBodyTask.Result;
      }

      Assert.IsNotNull(response, "Response was null");
      Assert.AreEqual(expectedStatusCode, response.StatusCode, "Status code didn't meet expectation");
      Assert.IsNotNull(messageBody, "Body was null");
      Assert.IsTrue(messageBody.StartsWith(expectedBodyPrefix), "Body didn't start with the expected prefix.  Expected to start with:\r\n"+ expectedBodyPrefix+ "\r\n, but actually started with:\r\n" +
        (messageBody.Length < expectedBodyPrefix.Length? messageBody : messageBody.Substring(0, expectedBodyPrefix.Length)));
    }
  }
}
