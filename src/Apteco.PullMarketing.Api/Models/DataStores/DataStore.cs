using System.ComponentModel.DataAnnotations;

namespace Apteco.PullMarketing.Api.Models.DataStores
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

    public static implicit operator Data.Models.DataStore(DataStore dataStore)
    {
      return new Data.Models.DataStore()
      {
        Name = dataStore.Name,
        PrimaryKeyFieldName = dataStore.PrimaryKeyFieldName
      };
    }

    public static implicit operator DataStore(Data.Models.DataStore dataStore)
    {
      return new DataStore()
      {
        Name = dataStore.Name,
        PrimaryKeyFieldName = dataStore.PrimaryKeyFieldName
      };
    }
  }
}
