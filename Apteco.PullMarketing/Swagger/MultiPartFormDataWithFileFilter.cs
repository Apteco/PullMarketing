using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Apteco.PullMarketing.ModelBinding;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Apteco.PullMarketing.Swagger
{
  //See glyons' post in https://github.com/domaindrivendev/Swashbuckle/issues/120
  public class MultiPartFormDataWithFileFilter : IOperationFilter
  {
    #region public methods
    public void Apply(Operation operation, OperationFilterContext context)
    {
      var swaggerFormAttributes = context.ApiDescription.ActionAttributes().OfType<MultiPartFormDataWithFileAttribute>();
      foreach (var swaggerFormAttribute in swaggerFormAttributes)
      {
        List<string> formFileParameters = new List<string>();
        foreach (ApiParameterDescription parameterDescription in context.ApiDescription.ParameterDescriptions)
        {
          if (parameterDescription.ModelMetadata.ContainerType == typeof(IFormFile))
          {
            formFileParameters.Add(parameterDescription.Name);
          }
        }

        if (operation.Parameters == null)
          operation.Parameters = new List<IParameter>();

        for (int i = operation.Parameters.Count - 1; i >= 0; i--)
        {
          if (formFileParameters.Contains(operation.Parameters[i].Name))
            operation.Parameters.RemoveAt(i);
          else if (operation.Parameters[i].In == "body")
            operation.Parameters[i].In = "formData";
        }

        operation.Parameters.Add(new NonBodyParameter()
        {
          In = "formData",
          Name = swaggerFormAttribute.Name,
          Description = swaggerFormAttribute.Description,
          Type = "file",
          Required = true,
        });

        if (operation.Consumes == null)
          operation.Consumes = new List<string>();

        operation.Consumes.Add("multipart/form-data");
      }
    }
    #endregion
  }
}