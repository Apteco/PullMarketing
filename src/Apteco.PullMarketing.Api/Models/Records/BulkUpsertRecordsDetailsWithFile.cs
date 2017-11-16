using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Apteco.PullMarketing.Api.ModelBinding;

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
    /// The BulkUpsertRecordsDetails
    /// </summary>
    [FromJson]
    [FromBody]
    public BulkUpsertRecordsDetails BulkUpsertRecordsDetails { get; set; }
  }
}
