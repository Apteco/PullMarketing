using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Apteco.PullMarketing.Api.Models.Records
{
  /// <summary>
  /// Details for bulk upserting a set of records
  /// </summary>
  public class BulkUpsertRecordsDetailsWithFile
  {
    /// <summary>
    /// The File
    /// </summary>
    public IFormFile File { get; set; }

    /// <summary>
    /// The list of the source fields
    /// </summary>
    [Required]
    public List<string> SourceFieldNames { get; set; }

    /// <summary>
    /// The list of the destination fields
    /// </summary>
    [Required]
    public List<string> DestinationFieldNames { get; set; }

    /// <summary>
    /// The key field name
    /// </summary>
    [Required]
    public string PrimaryKeyFieldName { get; set; }

    /// <summary>
    /// The encoding type of the input data file
    /// </summary>
    [Required]
    public FileEncodings Encoding { get; set; }

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
