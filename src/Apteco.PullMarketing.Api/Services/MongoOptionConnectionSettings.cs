using Apteco.PullMarketing.Data.Mongo;
using Microsoft.Extensions.Options;

namespace Apteco.PullMarketing.Services
{
  public class MongoOptionConnectionSettings : IMongoConnectionSettings
  {
    #region private fields
    private IOptionsMonitor<MongoConnectionSettings> innerConnectionSettings;
    #endregion

    #region public properties
    public string Hostname
    {
      get { return innerConnectionSettings.CurrentValue.Hostname; }
    }
    #endregion

    #region public constructor
    public MongoOptionConnectionSettings(IOptionsMonitor<MongoConnectionSettings> innerConnectionSettings)
    {
      this.innerConnectionSettings = innerConnectionSettings;
    }
    #endregion
  }
}
