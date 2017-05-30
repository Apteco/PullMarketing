namespace Apteco.PullMarketing.Data.Dynamo
{
  public interface IDynamoConnectionSettings
  {
    #region public properties
    string ServiceUrl { get; }
    string AccessKey { get; }
    string SecretAccessKey { get;}
    int ModifyDataStoreTimeoutInSeconds { get;}
    #endregion
  }
}
