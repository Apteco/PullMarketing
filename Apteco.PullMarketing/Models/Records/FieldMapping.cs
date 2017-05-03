using System.ComponentModel.DataAnnotations;

namespace Apteco.PullMarketing.Models.Records
{
  /// <summary>
  /// Details of the mapping from the field in a bulk upsert file to the field in a record
  /// </summary>
  public class FieldMapping
  {
    /// <summary>
    /// The name of the column in the file
    /// </summary>
    [Required]
    public string SourceFileFieldName { get; set; }

    /// <summary>
    /// The name of the field to create in the record
    /// </summary>
    [Required]
    public string DestinationRecordFieldName { get; set; }
  }
}
