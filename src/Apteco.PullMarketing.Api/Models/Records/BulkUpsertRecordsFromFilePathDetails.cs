using System.ComponentModel.DataAnnotations;

namespace Apteco.PullMarketing.Models.Records
{
  /// <summary>
  /// Details for bulk upserting a set of records
  /// </summary>
  public class BulkUpsertRecordsFromFilePathDetails : BulkUpsertRecordsDetails
  {
    /// <summary>
    /// The path to the file containing the records to upsert
    /// </summary>
    [Required]
    public string FilePath { get; set; }
  }
}
