using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Apteco.PullMarketing.Api.ModelBinding
{
  public class JsonModelBinderProvider : IModelBinderProvider
  {
    #region public methods
    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
      if (context == null)
        throw new ArgumentNullException(nameof(context));

      if (context.Metadata.IsComplexType)
      {
        var propName = context.Metadata.PropertyName;
        var propInfo = context.Metadata.ContainerType?.GetProperty(propName);
        if (propName == null || propInfo == null)
          return null;

        // Look for FromJson attributes
        var attribute = propInfo.GetCustomAttributes(typeof(FromJsonAttribute), false).FirstOrDefault();
        if (attribute != null)
          return new JsonModelBinder(context.Metadata.ModelType);
      }
      return null;
    }
    #endregion
  }
}
