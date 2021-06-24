using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Apteco.PullMarketing.Api.Swagger
{
  internal class OperationNameFilter : IOperationFilter
  {
    #region public methods
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
      operation.OperationId = context.ApiDescription.GroupName + "_" + operation.OperationId;
    }
    #endregion
  }
}
