using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Apteco.PullMarketing.Api.Models.Records
{
  /// <summary>
  /// Details for a record
  /// </summary>
  public class Record
  {
    /// <summary>
    /// The name of the data store the record is in
    /// </summary>
    [Required]
    public string DataStoreName { get; set; }

    /// <summary>
    /// The record's primary key
    /// </summary>
    [Required]
    public string Key { get; set; }

    /// <summary>
    /// The fields for this record
    /// </summary>
    public List<Field> Fields { get; set; }
  }
}
