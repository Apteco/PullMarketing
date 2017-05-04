using System.IO;
using System.Threading.Tasks;
using Apteco.PullMarketing.Data;

namespace Apteco.PullMarketing.Services
{
  public interface IDataService
  {
    #region public methods
    Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails);
    #endregion
  }
}
