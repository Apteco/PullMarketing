using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ApiPager.Core;
using ApiPager.Data.Linq;
using Apteco.PullMarketing.Data;
using Apteco.PullMarketing.Data.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Apteco.PullMarketing.Services
{
  public class MongoService : IDataService
  {
    #region private fields
    private IOptions<MongoConnectionSettings> connectionSettings;
    private ILoggerFactory loggerFactory;
    #endregion

    #region public constructor
    public MongoService(IOptions<MongoConnectionSettings> connectionSettings, ILoggerFactory loggerFactory)
    {
      this.connectionSettings = connectionSettings;
      this.loggerFactory = loggerFactory;
    }
    #endregion

    #region public methods
    public async Task<IEnumerable<DataStore>> GetDataStores(FilterPageAndSortInfo filterPageAndSortInfo)
    {
      MongoFacade facade = CreateMongoFacade();
      List<DataStore> dataStores = await facade.GetDataStores();
      return dataStores.Filter(filterPageAndSortInfo, new string[] { "Name", "PrimaryKeyFieldName" }, "Name");
    }

    public async Task<DataStore> GetDataStore(string name)
    {
      MongoFacade facade = CreateMongoFacade();
      return await facade.GetDataStore(name);
    }

    public async Task<bool> CreateDataStore(DataStore dataStore)
    {
      MongoFacade facade = CreateMongoFacade();
      return await facade.CreateTable(dataStore.Name, dataStore.PrimaryKeyFieldName, 120);
    }

    public async Task<bool> DeleteDataStore(string name)
    {
      MongoFacade facade = CreateMongoFacade();
      return await facade.DeleteTable(name, 120);
    }

    public async Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails)
    {
      MongoFacade facade = CreateMongoFacade();
      return await facade.Upsert(stream, upsertDetails);
    }

    public async Task<List<Record>> GetRecords(string dataStoreName, FilterPageAndSortInfo filterPageAndSortInfo)
    {
      MongoFacade facade = CreateMongoFacade();
      return await facade.GetRecords(dataStoreName, filterPageAndSortInfo);
    }

    public async Task<Record> GetRecord(string dataStoreName, string primaryKeyValue)
    {
      MongoFacade facade = CreateMongoFacade();
      return await facade.GetRecord(dataStoreName, primaryKeyValue);
    }

    public async Task<bool> UpsertRecord(string dataStoreName, Record record)
    {
      MongoFacade facade = CreateMongoFacade();
      return await facade.UpsertRecord(dataStoreName, record);
    }

    public async Task<bool> DeleteRecord(string dataStoreName, string primaryKeyValue)
    {
      MongoFacade facade = CreateMongoFacade();
      return await facade.DeleteRecord(dataStoreName, primaryKeyValue);
    }

    public async Task<Field> GetRecordField(string dataStoreName, string primaryKeyValue, string fieldName)
    {
      MongoFacade facade = CreateMongoFacade();
      return await facade.GetRecordField(dataStoreName, primaryKeyValue, fieldName);
    }

    public async Task<bool> UpsertRecordField(string dataStoreName, string primaryKeyValue, Field field)
    {
      MongoFacade facade = CreateMongoFacade();
      return await facade.UpsertRecordField(dataStoreName, primaryKeyValue, field);
    }

    public async Task<bool> DeleteRecordField(string dataStoreName, string primaryKeyValue, string fieldName)
    {
      MongoFacade facade = CreateMongoFacade();
      return await facade.DeleteRecordField(dataStoreName, primaryKeyValue, fieldName);
    }
    #endregion

    #region private methods
    private MongoFacade CreateMongoFacade()
    {
      return new MongoFacade(connectionSettings.Value, loggerFactory.CreateLogger<MongoFacade>());
    }
    #endregion
  }
}
