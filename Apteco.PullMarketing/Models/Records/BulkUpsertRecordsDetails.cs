using System.Collections.Generic;

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
    public List<FieldMapping> FieldMappings { get; set; }
  }
}
