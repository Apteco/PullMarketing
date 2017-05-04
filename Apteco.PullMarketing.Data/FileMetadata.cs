using System.Collections.Generic;

namespace Apteco.PullMarketing.Data
{
  public class FileMetadata
  {
    #region public properties
    public bool Header { get; set; }
    public bool MatchOnHeader { get; set; }
    public int Delimiter { get; set; }
    public string Encoding { get; set; }
    public List<FieldMetadata> Fields { get; set; }
    #endregion
  }
}
