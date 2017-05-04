using System.IO;
using System.Threading.Tasks;
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
    public async Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails)
    {
      MongoFacade facade = new MongoFacade(connectionSettings.Value, loggerFactory.CreateLogger<MongoFacade>());
      return await facade.Upsert(stream, upsertDetails);
    }
    #endregion
  }
}
