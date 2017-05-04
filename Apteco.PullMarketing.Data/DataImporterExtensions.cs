using System.IO;
using System.Threading.Tasks;

namespace Apteco.PullMarketing.Data
{
  public static class DataImporterExtensions
  {
    #region public methods
    public static async Task<UpsertResults> DoImport(this IDataFacade dataFacade, string filename, UpsertDetails upsertDetails)
    {
      using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
      {
        return await dataFacade.Upsert(fs, upsertDetails);
      }
    }
    #endregion
  }
}
