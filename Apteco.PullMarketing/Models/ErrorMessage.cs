using System.Collections.Generic;
using System.Linq;

namespace Apteco.PullMarketing.Models
{
  /// <summary>
  /// Details of an error that has occurred in the API
  /// </summary>
  public class ErrorMessage
  {
    /// <summary>
    /// If present a code number for this type of error
    /// </summary>
    public int? Code { get; set; }

    /// <summary>
    /// If present an id which can be looked up by an administrator on the server-side for more details
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// If present a message describing the error
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// If present a list of parameters associated with this error
    /// </summary>
    public List<ErrorMessageParameter> Parameters { get; set; }

    public ErrorMessage()
    {
    }

    public ErrorMessage(ErrorMessageCodes code, string message)
      : this(null, code, message)
    {
    }

    public ErrorMessage(int? id, ErrorMessageCodes code, string message)
      : this(id, code, message, new ErrorMessageParameter[0])
    {
    }

    public ErrorMessage(int? id, ErrorMessageCodes code, string message, params ErrorMessageParameter[] parameters)
    {
      Id = id;
      Code = (int) code;
      Message = message;
      Parameters = parameters.ToList();
    }
  }
}
