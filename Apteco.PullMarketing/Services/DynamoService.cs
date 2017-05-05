using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApiPager.Core;
using ApiPager.Data.Linq;
using Apteco.PullMarketing.Data;
using Apteco.PullMarketing.Data.Dynamo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Apteco.PullMarketing.Services
{
  public class DynamoService : IDataService
  {
    #region private fields
    private IOptions<DynamoConnectionSettings> connectionSettings;
    private ILoggerFactory loggerFactory;
    #endregion

    #region public constructor
    public DynamoService(IOptions<DynamoConnectionSettings> connectionSettings, ILoggerFactory loggerFactory)
    {
      this.connectionSettings = connectionSettings;
      this.loggerFactory = loggerFactory;
    }
    #endregion

    #region public methods
    public async Task<IEnumerable<DataStore>> GetDataStores(FilterPageAndSortInfo filterPageAndSortInfo)
    {
      DynamoFacade facade = CreateDynamoFacade();
      List<DataStore> dataStores = await facade.GetDataStores();
      return dataStores.Filter(filterPageAndSortInfo, new string[] { "Name", "PrimaryKeyFieldName" }, "Name");
    }

    public async Task<DataStore> GetDataStore(string name)
    {
      DynamoFacade facade = CreateDynamoFacade();
      return await facade.GetDataStore(name);
    }

    public async Task<bool> CreateDataStore(DataStore dataStore)
    {
      DynamoFacade facade = CreateDynamoFacade();
      return await facade.CreateTable(dataStore.Name, dataStore.PrimaryKeyFieldName, 120);
    }

    public async Task<bool> DeleteDataStore(string name)
    {
      DynamoFacade facade = CreateDynamoFacade();
      return await facade.DeleteTable(name, 120);
    }

    public async Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails)
    {
      DynamoFacade facade = CreateDynamoFacade();
      return await facade.Upsert(stream, upsertDetails);
    }
    #endregion

    #region private methods
    private DynamoFacade CreateDynamoFacade()
    {
      return new DynamoFacade(connectionSettings.Value, loggerFactory.CreateLogger<DynamoFacade>());
    }
    #endregion
  }
}
