using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
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

    public static object GetPropertyValue(object obj, string propertyName)
    {
      var property = obj.GetType().GetProperties().FirstOrDefault(x => x.Name == propertyName);
      if (property == null)
        throw new Exception("The given object ("+obj+") doesn't have a property called \""+propertyName+"\"");

      return property.GetValue(obj);
    }

    public static object IsAnonymousTypeWithProperties(params string[] expectedPropertyNames)
    {
      if (expectedPropertyNames == null)
        expectedPropertyNames = new string[0];

      return Match.Create(
        (object actual) =>
        {
          if (actual == null)
            return false;

          var actualPropertyNames = actual.GetType().GetProperties().Select(x => x.Name);

          return expectedPropertyNames.SequenceEqual(actualPropertyNames);
        });
    }
    public static object IsAnonymousType(object expected)
    {
      return Match.Create(
        (object actual) =>
        {
          if (expected == null)
          {
            if (actual == null)
              return true;
            else
              return false;
          }
          else if (actual == null)
            return false;

          var expectedPropertyNames = expected.GetType().GetProperties().Select(x => x.Name);
          var expectedPropertyValues = expected.GetType().GetProperties().Select(x => x.GetValue(expected, null));
          var actualPropertyNames = actual.GetType().GetProperties().Select(x => x.Name);
          var actualPropertyValues = actual.GetType().GetProperties().Select(x => x.GetValue(actual, null));

          return expectedPropertyNames.SequenceEqual(actualPropertyNames)
                 && expectedPropertyValues.SequenceEqual(actualPropertyValues);
        });
    }
  }
}
