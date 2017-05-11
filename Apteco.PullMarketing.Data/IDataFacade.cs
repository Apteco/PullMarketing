using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ApiPager.Core;

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
    Task<List<Record>> GetRecords(string tableName, FilterPageAndSortInfo filterPageAndSortInfo);
    Task<Record> GetRecord(string tableName, string primaryKeyValue);
    Task<bool> UpsertRecord(string tableName, Record record);
    Task<bool> DeleteRecord(string tableName, string primaryKeyValue);
    Task<Field> GetRecordField(string tableName, string primaryKeyValue, string fieldName);
    Task<bool> UpsertRecordField(string tableName, string primaryKeyValue, Field field);
    Task<bool> DeleteRecordField(string tableName, string primaryKeyValue, string fieldName);
    #endregion
  }
}
