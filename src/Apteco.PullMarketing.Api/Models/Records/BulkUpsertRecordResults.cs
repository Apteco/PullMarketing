﻿using System.ComponentModel.DataAnnotations;

namespace Apteco.PullMarketing.Api.Models.Records
{
  /// <summary>
  /// Details for the results of a record bulk upsert
  /// </summary>
  public class BulkUpsertRecordResults
  {
    /// <summary>
    /// The name of the data store that was upserted into
    /// </summary>
    [Required]
    public string DataStoreName { get; set; }

    /// <summary>
    /// The number of records upserted
    /// </summary>
    [Required]
    public long NumberOfRecordsUpserted { get; set; }

    /// <summary>
    /// The number of records skipped
    /// </summary>
    [Required]
    public long NumberOfRecordsSkipped { get; set; }
  }
}
