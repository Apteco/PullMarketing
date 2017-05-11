using Apteco.PullMarketing.Data.Dynamo;
using Microsoft.Extensions.Options;

namespace Apteco.PullMarketing.Services
{
  public class DynamoOptionConnectionSettings : IDynamoConnectionSettings
  {
    #region private fields
    private IOptionsMonitor<DynamoConnectionSettings> innerConnectionSettings;
    #endregion

    #region public properties
    public string ServiceUrl
    {
      get { return innerConnectionSettings.CurrentValue.ServiceUrl; }
    }

    public string AccessKey
    {
      get { return innerConnectionSettings.CurrentValue.AccessKey; }
    }

    public string SecretAccessKey
    {
      get { return innerConnectionSettings.CurrentValue.SecretAccessKey; }
    }

    public int ModifyDataStoreTimeoutInSeconds
    {
      get { return innerConnectionSettings.CurrentValue.ModifyDataStoreTimeoutInSeconds; }
    }
    #endregion

    #region public constructor
    public DynamoOptionConnectionSettings(IOptionsMonitor<DynamoConnectionSettings> innerConnectionSettings)
    {
      this.innerConnectionSettings = innerConnectionSettings;
    }
    #endregion
  }
}
