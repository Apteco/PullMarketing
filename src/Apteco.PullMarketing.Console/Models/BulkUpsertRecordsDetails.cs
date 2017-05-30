using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Apteco.PullMarketing.Console.Models
{
  public class BulkUpsertRecordsDetails
  {
    #region public properties
    public List<FieldMapping> FieldMappings { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public FileEncodings Encoding { get; set;  }

    public string Delimiter { get; set; }

    public string Encloser { get; set; }
    #endregion
  }
}
