using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ApiPager.Core;
using Apteco.PullMarketing.Data.Models;

namespace Apteco.PullMarketing.Data
{
  public interface IDataService
  {
    #region public methods
    Task<IEnumerable<DataStore>> GetDataStores(FilterPageAndSortInfo filterPageAndSortInfo);
    Task<DataStore> GetDataStore(string name);
    Task<bool> CreateDataStore(DataStore dataStore);
    Task<bool> DeleteDataStore(string name);
    Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails);
    Task<IEnumerable<Record>> GetRecords(string tableName, FilterPageAndSortInfo filterPageAndSortInfo);
    Task<Record> GetRecord(string tableName, string primaryKeyValue);
    Task<bool> UpsertRecord(string tableName, Record record);
    Task<bool> DeleteRecord(string tableName, string primaryKeyValue);
    Task<Field> GetRecordField(string tableName, string primaryKeyValue, string fieldName);
    Task<bool> UpsertRecordField(string tableName, string primaryKeyValue, Field field);
    Task<bool> DeleteRecordField(string tableName, string primaryKeyValue, string fieldName);
    #endregion
  }
}
