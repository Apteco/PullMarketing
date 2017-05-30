namespace Apteco.PullMarketing.Console.Models
{
  public class FieldMapping
  {
    #region public properties
    public string SourceFileFieldName { get; set; }
    public string DestinationRecordFieldName { get; set; }
    public bool IsPrimaryKeyField { get; set; }
    #endregion
  }
}
