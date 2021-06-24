using System.Collections.Generic;
using System.Text;
using ApiPager.Core;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Apteco.PullMarketing.Api.Swagger
{
  public class AddFilterPageAndSortOperationFilter : IOperationFilter
  {
    #region private constants
    private const string IntegerParameterType = "integer";
    private const string StringParameterType = "string";
    private const string ArrayParameterType = "array";
    #endregion

    #region public methods
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
      CanFilterPageAndSortAttribute filterPageAndSort = context.ApiDescription.GetFirstMethodAttribute<CanFilterPageAndSortAttribute>();

      if (filterPageAndSort != null)
      {
        if (operation.Parameters == null)
          operation.Parameters = new List<OpenApiParameter>();

        StringBuilder listOfFieldsDescriptionBuilder = new StringBuilder();
        StringBuilder listOfFieldsAndFunctionsDescriptionBuilder = new StringBuilder();
        if (filterPageAndSort.AvailableFields != null && filterPageAndSort.AvailableFields.Length > 0)
        {
          listOfFieldsDescriptionBuilder
            .Append("The available list of fields are ").Append(string.Join(", ", filterPageAndSort.AvailableFields)).Append(".");

          listOfFieldsAndFunctionsDescriptionBuilder.Append(listOfFieldsDescriptionBuilder);

          if (filterPageAndSort.AvailableFields != null && filterPageAndSort.AvailableFields.Length > 0)
          {
            listOfFieldsAndFunctionsDescriptionBuilder
              .Append(" The following functions can also be used in the filter: ").Append(string.Join(", ", filterPageAndSort.AvailableFields)).Append(".");
          }
        }

        operation.Parameters.Add(CreateQueryParameter("filter", StringParameterType, "Filter the list of items using a simple expression language.  " + listOfFieldsAndFunctionsDescriptionBuilder));
        operation.Parameters.Add(CreateQueryParameter("orderBy", StringParameterType, "Order the items by a given field (in ascending order unless the field is preceeded by a \"-\" character).  " + listOfFieldsDescriptionBuilder));
        operation.Parameters.Add(CreateQueryParameter("offset", IntegerParameterType, "The number of items to skip in the (potentially filtered) result set before returning subsequent items."));
        operation.Parameters.Add(CreateQueryParameter("count", IntegerParameterType, "The maximum number of items to show from the (potentially filtered) result set."));
      }
    }
    #endregion

    #region private methods

    private OpenApiParameter CreateQueryParameter(string name, string type, string description)
    {
      OpenApiParameter parameter = new OpenApiParameter
      {
        In = ParameterLocation.Query,
        Name = name,
        Description = description,
        //Type = type,
        Required = false,
      };
/*
      if (type == IntegerParameterType)
        parameter.Minimum = 0;
      else if (type == ArrayParameterType)
      {
        parameter.Items = new PartialSchema() { Type = "string" };
        parameter.CollectionFormat = "multi";
      }
*/
      return parameter;
    }
    #endregion
  }
}
