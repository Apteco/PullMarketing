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
    Task<List<Record>> GetRecords(string dataStoreName, FilterPageAndSortInfo filterPageAndSortInfo);
    Task<Record> GetRecord(string dataStoreName, string primaryKeyValue);
    Task<bool> UpsertRecord(string dataStoreName, Record record);
    Task<bool> DeleteRecord(string dataStoreName, string primaryKeyValue);
    Task<Field> GetRecordField(string dataStoreName, string primaryKeyValue, string fieldName);
    Task<bool> UpsertRecordField(string dataStoreName, string primaryKeyValue, Field field);
    Task<bool> DeleteRecordField(string dataStoreName, string primaryKeyValue, string fieldName);

    #endregion
  }
}
