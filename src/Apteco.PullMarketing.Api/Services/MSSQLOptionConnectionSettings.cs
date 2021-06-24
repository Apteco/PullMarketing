using Apteco.PullMarketing.Data.MSSQL;
using Microsoft.Extensions.Options;

namespace Apteco.PullMarketing.Api.Services
{
  public class MSSQLOptionConnectionSettings : IMSSQLConnectionSettings
  {
    #region private fields
    private IOptionsMonitor<MSSQLConnectionSettings> innerConnectionSettings;
    #endregion

    #region public properties
    public string ConnectionString
    {
      get { return innerConnectionSettings.CurrentValue.ConnectionString; }
    }
    #endregion

    #region public constructor
    public MSSQLOptionConnectionSettings(IOptionsMonitor<MSSQLConnectionSettings> innerConnectionSettings)
    {
      this.innerConnectionSettings = innerConnectionSettings;
    }
    #endregion
  }
}
