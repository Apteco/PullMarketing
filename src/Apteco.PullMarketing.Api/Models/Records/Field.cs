using System.ComponentModel.DataAnnotations;

namespace Apteco.PullMarketing.Api.Models.Records
{
  /// <summary>
  /// A key value pair for a record
  /// </summary>
  public class Field
  {
    /// <summary>
    /// The field's key
    /// </summary>
    [Required]
    public string Key { get; set; }

    /// <summary>
    /// The field's value
    /// </summary>
    public string Value { get; set; }
  }
}
