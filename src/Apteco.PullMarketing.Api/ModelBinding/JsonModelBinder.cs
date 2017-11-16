using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace Apteco.PullMarketing.Api.ModelBinding
{
  public class JsonModelBinder : IModelBinder
  {
    #region private fields
    private Type targetType;
    #endregion

    #region public methods
    public JsonModelBinder(Type type)
    {
      if (type == null)
        throw new ArgumentNullException(nameof(type));

      targetType = type;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
      if (bindingContext == null)
        throw new ArgumentNullException(nameof(bindingContext));

      // Check the value sent in
      var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
      if (valueProviderResult != ValueProviderResult.None)
      {
        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
        // Attempt to convert the input value
        var valueAsString = valueProviderResult.FirstValue;
        object result;
        bool success = TryConvert(valueAsString, targetType, out result);
        if (success)
        {
          bindingContext.Result = ModelBindingResult.Success(result);
          return Task.CompletedTask;
        }
      }
      return Task.CompletedTask;
    }
    #endregion

    #region private methods
    private bool TryConvert(string modelValue, Type targetType, out object value)
    {
      try
      {
        value = JsonConvert.DeserializeObject(modelValue, targetType);
        return value != null;
      }
      catch
      {
        value = null;
        return false;
      }
    }
    #endregion
  }
}
