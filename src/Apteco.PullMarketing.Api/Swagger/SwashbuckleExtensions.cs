using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Apteco.PullMarketing.Api.Swagger
{
  public static class SwashbuckleExtensions
  {
    public static TAttribute GetFirstMethodAttribute<TAttribute>(this ApiDescription apiDescription) where TAttribute : Attribute
    {
      MethodInfo methodInfo = null;
      if ((apiDescription?.TryGetMethodInfo(out methodInfo) != true) || methodInfo == null)
        return null;

      return methodInfo.GetCustomAttributes().OfType<TAttribute>().FirstOrDefault();
    }
  }
}
