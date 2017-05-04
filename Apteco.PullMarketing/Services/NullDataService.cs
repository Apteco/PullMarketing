using System;
using System.IO;
using System.Threading.Tasks;
using Apteco.PullMarketing.Data;

namespace Apteco.PullMarketing.Services
{
  public class NullDataService : IDataService
  {
    #region public methods
    public Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails)
    {
      throw new Exception("NullDataService doesn't support importing data");
    }
    #endregion
  }
}
