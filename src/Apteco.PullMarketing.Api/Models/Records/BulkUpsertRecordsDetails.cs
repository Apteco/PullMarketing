using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Apteco.PullMarketing.Models.Records
{
  /// <summary>
  /// Details for bulk upserting a set of records
  /// </summary>
  public class BulkUpsertRecordsDetails
  {
    /// <summary>
    /// The list of field mappings from the source file to the record
    /// </summary>
    [Required]
    public List<FieldMapping> FieldMappings { get; set; }

    /// <summary>
    /// The encoding type of the input data file
    /// </summary>
    [Required]
    public FileEncodings Encoding { get; set;  }

    /// <summary>
    /// The delimiter used in the input data file
    /// </summary>
    [Required]
    public string Delimiter { get; set; }
  }
}
