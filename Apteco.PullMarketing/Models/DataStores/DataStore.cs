using System.ComponentModel.DataAnnotations;

namespace Apteco.PullMarketing.Models.DataStores
{
  /// <summary>
  /// Details for a data store
  /// </summary>
  public class DataStore
  {
    /// <summary>
    /// The data stores's name
    /// </summary>
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// The primary key field for this data store
    /// </summary>
    [Required]
    public string PrimaryKeyFieldName { get; set; }
  }
}
