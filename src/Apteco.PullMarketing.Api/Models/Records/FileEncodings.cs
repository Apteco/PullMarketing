using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Apteco.PullMarketing.Api.Models.Records
{
  /// <summary>
  /// The types of supported input data encoding
  /// </summary>
  public enum FileEncodings
  {
    /// <summary>
    /// The current ANSI code page encoding
    /// </summary>
    Default,
    
    /// <summary>
    /// ASCII encoding
    /// </summary>
    ASCII,

    /// <summary>
    /// UTF-8 encoding
    /// </summary>
    UTF8
  }
}
