using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Apteco.PullMarketing.Models.Records
{
  /// <summary>
  /// Details for bulk upserting a set of records
  /// </summary>
  public class BulkUpsertRecordsFromFilePathDetails
  {
    /// <summary>
    /// The path to the file containing the records to upsert
    /// </summary>
    [Required]
    public string FilePath { get; set; }

    /// <summary>
    /// The list of field mappings from the source file to the record
    /// </summary>
    public List<FieldMapping> FieldMappings { get; set; }
  }
}
