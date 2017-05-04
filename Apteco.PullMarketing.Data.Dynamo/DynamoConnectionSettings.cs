using System;
using System.Collections.Generic;
using System.Text;

namespace Apteco.PullMarketing.Data.Dynamo
{
  public class DynamoConnectionSettings
  {
    #region public properties
    public string ServiceUrl { get; set; }
    public string AccessKey { get; set; }
    public string SecretAccessKey { get; set; }
    #endregion
  }
}
