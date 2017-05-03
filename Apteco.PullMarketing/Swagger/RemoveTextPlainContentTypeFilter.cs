using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Apteco.PullMarketing.Swagger
{
  internal class RemoveTextPlainContentTypeFilter : IOperationFilter
  {
    #region public methods
    public void Apply(Operation operation, OperationFilterContext context)
    {
      if (operation.Produces != null)
      {
        operation.Produces.Remove("text/plain");
      }
    }
    #endregion
  }
}
