using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ApiPager.Core;
using Apteco.PullMarketing.Data.Models;

namespace Apteco.PullMarketing.Data
{
  public class NullDataService : IDataService
  {
    #region public methods
    public Task<IEnumerable<DataStore>> GetDataStores(FilterPageAndSortInfo filterPageAndSortInfo)
    {
      throw new NotImplementedException();
    }

    public Task<DataStore> GetDataStore(string name)
    {
      throw new NotImplementedException();
    }

    public Task<bool> CreateDataStore(DataStore dataStore)
    {
      throw new NotImplementedException();
    }

    public Task<bool> DeleteDataStore(string name)
    {
      throw new NotImplementedException();
    }

    public Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails)
    {
      throw new NotImplementedException();
    }

    public Task<IEnumerable<Record>> GetRecords(string dataStoreName, FilterPageAndSortInfo filterPageAndSortInfo)
    {
      throw new NotImplementedException();
    }

    public Task<Record> GetRecord(string dataStoreName, string primaryKeyValue)
    {
      throw new NotImplementedException();
    }

    public Task<bool> UpsertRecord(string dataStoreName, Record record)
    {
      throw new NotImplementedException();
    }

    public Task<bool> DeleteRecord(string dataStoreName, string primaryKeyValue)
    {
      throw new NotImplementedException();
    }

    public Task<Field> GetRecordField(string dataStoreName, string primaryKeyValue, string fieldName)
    {
      throw new NotImplementedException();
    }

    public Task<bool> UpsertRecordField(string dataStoreName, string primaryKeyValue, Field field)
    {
      throw new NotImplementedException();
    }

    public Task<bool> DeleteRecordField(string dataStoreName, string primaryKeyValue, string fieldName)
    {
      throw new NotImplementedException();
    }
    #endregion
  }
}
