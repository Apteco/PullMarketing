namespace Apteco.PullMarketing.Data
{
  public class UpsertDetails
  {
    #region public properties
    public string TableName { get; set; }
    public string PrimaryKeyFieldName { get; set; }
    public FileMetadata FileMetadata { get; set; }
    #endregion
  }
}
