using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ApiPager.Core;
using Apteco.PullMarketing.Data;

namespace Apteco.PullMarketing.Services
{
  public interface IDataService
  {
    #region public methods
    Task<IEnumerable<DataStore>> GetDataStores(FilterPageAndSortInfo filterPageAndSortInfo);
    Task<DataStore> GetDataStore(string name);
    Task<bool> CreateDataStore(DataStore dataStore);
    Task<bool> DeleteDataStore(string name);

    Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails);
    #endregion
  }
}
