using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Apteco.PullMarketing.Services
{
  public class RoutingService : IRoutingService
  {
    #region public methods
    public Uri GetAbsoluteRouteUrl(Controller controller, string routeName)
    {
      IUrlHelper urlHelper = controller.Url;
      HttpRequest request = controller.Request;

      var routeUrl = urlHelper.RouteUrl(routeName);
      var absUrl = string.Format("{0}://{1}{2}", request.Scheme, request.Host, routeUrl);
      return new Uri(absUrl, UriKind.Absolute);
    }

    public Uri GetAbsoluteRouteUrl(Controller controller, string routeName, object values)
    {
      IUrlHelper urlHelper = controller.Url;
      HttpRequest request = controller.Request;

      var routeUrl = urlHelper.RouteUrl(routeName, values);
      var absUrl = string.Format("{0}://{1}{2}", request.Scheme, request.Host, routeUrl);
      return new Uri(absUrl, UriKind.Absolute);
    }
    #endregion
  }
}
