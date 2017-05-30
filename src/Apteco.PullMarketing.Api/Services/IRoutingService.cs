using System;
using Microsoft.AspNetCore.Mvc;

namespace Apteco.PullMarketing.Services
{
  public interface IRoutingService
  {
    #region public methods
    Uri GetAbsoluteRouteUrl(Controller controller, string routeName);
    Uri GetAbsoluteRouteUrl(Controller controller, string routeName, object values);
    #endregion
  }
}
