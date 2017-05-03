using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Apteco.PullMarketing.Swagger
{
  internal class FixFormInParameterFilter : IOperationFilter
  {
    #region public methods
    public void Apply(Operation operation, OperationFilterContext context)
    {
      if (operation.Parameters != null)
      {
        foreach (var parameter in operation.Parameters)
        {
          if (parameter.In == "form")
            parameter.In = "formData";
        }
      }
    }
    #endregion
  }
}
