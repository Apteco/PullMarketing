using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Apteco.PullMarketing.Api.Models
{
  /// <summary>
  /// Details of any errors that have occurred in the API
  /// </summary>
  public class ErrorMessages
  {
    /// <summary>
    /// The list of errors that have occurred
    /// </summary>
    [Required]
    public List<ErrorMessage> Errors { get; set; }

    public ErrorMessages()
    {
    }

    public ErrorMessages(List<ErrorMessage> errors)
    {
      Errors = errors;
    }

    public ErrorMessages(params ErrorMessage[] errors)
    {
      Errors = errors.ToList();
    }
  }
}
