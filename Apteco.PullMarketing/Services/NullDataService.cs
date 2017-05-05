using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ApiPager.Core;
using Apteco.PullMarketing.Data;

namespace Apteco.PullMarketing.Services
{
  public class NullDataService : IDataService
  {
    #region public methods
    public Task<IEnumerable<DataStore>> GetDataStores(FilterPageAndSortInfo filterPageAndSortInfo)
    {
      throw new Exception("NullDataService doesn't support querying data store information");
    }

    public Task<DataStore> GetDataStore(string name)
    {
      throw new Exception("NullDataService doesn't support querying data store information");
    }

    public Task<bool> CreateDataStore(DataStore dataStore)
    {
      throw new Exception("NullDataService doesn't support creating data stores");
    }

    public Task<bool> DeleteDataStore(string name)
    {
      throw new Exception("NullDataService doesn't support deleting data stores");
    }

    public Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails)
    {
      throw new Exception("NullDataService doesn't support importing data");
    }
    #endregion
  }
}
