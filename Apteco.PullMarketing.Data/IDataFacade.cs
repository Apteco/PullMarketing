using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Apteco.PullMarketing.Data
{
  public interface IDataFacade
  {
    #region public methods
    Task<List<DataStore>> GetDataStores();
    Task<DataStore> GetDataStore(string name);
    Task<bool> CreateTable(string tableName, string primaryKeyFieldName, int timeoutInSeconds);
    Task<bool> DeleteTable(string tableName, int timeoutInSeconds);
    Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails);
    #endregion
  }
}
