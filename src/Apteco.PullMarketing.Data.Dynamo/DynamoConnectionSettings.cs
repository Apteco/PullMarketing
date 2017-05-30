namespace Apteco.PullMarketing.Data.Dynamo
{
  public class DynamoConnectionSettings : IDynamoConnectionSettings
  {
    #region public properties
    public string ServiceUrl { get; set; }
    public string AccessKey { get; set; }
    public string SecretAccessKey { get; set; }
    public int ModifyDataStoreTimeoutInSeconds { get; set; }
    #endregion
  }
}
