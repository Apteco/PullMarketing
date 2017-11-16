using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Apteco.PullMarketing.Api.Test.TestHelpers
{
  public class TestServerBuilder
  {
    #region private fields
    private List<KeyValuePair<string, string>> keyValuePairs;
    private MutableMemoryConfigurationSource mutableMemoryConfigurationSource;
    #endregion

    #region public constructor
    public TestServerBuilder()
    {
      keyValuePairs = new List<KeyValuePair<string, string>>();
    }
    #endregion

    #region public methods
    public TestServerBuilder WithConfigurationValue(string key, string value)
    {
      keyValuePairs.Add(new KeyValuePair<string, string>(key, value));
      return this;
    }

    public void UpdateConfigurationValue(string key, string value)
    {
      if (mutableMemoryConfigurationSource == null)
        throw new Exception("Can't update configuration before it has been built");

      mutableMemoryConfigurationSource[key] = value;
    }

    public TestServer Build()
    {
      IConfigurationRoot configuration = BuildConfiguration();

      var builder = new WebHostBuilder()
        .ConfigureServices(s => s.AddSingleton<TestConfiguration>(new TestConfiguration() { Configuration = configuration }))
        .UseContentRoot(@"..\..\..\..\..\src\Apteco.PullMarketing.Api")
        .UseStartup<TestStartup>();

      return new TestServer(builder);
    }
    #endregion

    #region private methods
    private IConfigurationRoot BuildConfiguration()
    {
      Uri uri = new Uri(GetType().GetTypeInfo().Assembly.CodeBase, UriKind.Absolute);
      string assemblyLocalPath = uri.LocalPath;
      string appSettingsPath = Path.Combine(Path.GetDirectoryName(assemblyLocalPath), @"..\..\..\..\..\src\Apteco.PullMarketing.Api\appsettings.json");

      mutableMemoryConfigurationSource = new MutableMemoryConfigurationSource(keyValuePairs);

      return new ConfigurationBuilder()
        .AddJsonFile(appSettingsPath)
        .Add(mutableMemoryConfigurationSource)
        .Build();
    }
    #endregion
  }
}
