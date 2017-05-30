using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Apteco.PullMarketing.Models
{
  /// <summary>
  /// A key/value pair for details of a particular error message
  /// </summary>
  public class ErrorMessageParameter
  {
    /// <summary>
    /// The key of the error message parameter
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The value of the error message parameter
    /// </summary>
    public string Value { get; set; }
  }
}
