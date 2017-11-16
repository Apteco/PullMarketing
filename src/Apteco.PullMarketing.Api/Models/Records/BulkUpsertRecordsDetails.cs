using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Apteco.PullMarketing.Api.Models.Records
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

    /// <summary>
    /// The character used to enclose fields within the input data file
    /// If there is no encloser then leave blank
    /// </summary>
    [Required]
    public string Encloser { get; set; }
  }
}
