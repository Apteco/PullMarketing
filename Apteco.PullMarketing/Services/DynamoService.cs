using System.IO;
using System.Threading.Tasks;
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
    public async Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails)
    {
      DynamoFacade facade = new DynamoFacade(connectionSettings.Value, loggerFactory.CreateLogger<DynamoFacade>());
      return await facade.Upsert(stream, upsertDetails);
    }
    #endregion
  }
}
