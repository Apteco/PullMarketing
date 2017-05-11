using System.Collections.Generic;

namespace Apteco.PullMarketing.Data.Models
{
  public class Record
  {
    public string Key { get; set; }
    public List<Field> Fields { get; set; }
  }
}
