using System.Collections.Generic;

namespace Apteco.PullMarketing.Models.Records
{
  /// <summary>
  /// Details for a record to upsert
  /// </summary>
  public class UpsertRecordDetails
  {
    /// <summary>
    /// The fields to upsert for this record
    /// </summary>
    public List<Field> Fields { get; set; }
  }
}
