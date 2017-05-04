using System.IO;
using System.Threading.Tasks;

namespace Apteco.PullMarketing.Data
{
  public interface IDataFacade
  {
    #region public methods
    Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails);
    #endregion
  }
}
