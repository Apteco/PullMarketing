using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiPager.AspNetCore;
using ApiPager.Core;
using ApiPager.Core.Models;
using Apteco.PullMarketing.Data;
using Apteco.PullMarketing.Data.Dynamo;
using Apteco.PullMarketing.Api.Models;
using Apteco.PullMarketing.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using DataStore = Apteco.PullMarketing.Api.Models.DataStores.DataStore;

namespace Apteco.PullMarketing.Api.Controllers
{
  /// <summary>
  /// An endpoint within the API to manipulate data stores
  /// </summary>
  [Route("api/[controller]")]
  public class DataStoresController : Controller
  {
    private IDataService dataService;
    private IRoutingService routingService;

    public DataStoresController(IDataService dataService, IRoutingService routingService)
    {
      this.dataService = dataService;
      this.routingService = routingService;
    }

    /// <summary>
    /// Returns the details of all the defined data stores
    /// </summary>
    /// <returns>Details for each defined data store</returns>
    /// <response code="200">The list of all data stores</response>
    /// <response code="400">A bad request</response>
    [HttpGet("", Name = "GetDataStores")]
    [ProducesResponseType(typeof(PagedResults<DataStore>), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [CanFilterPageAndSort(new string[] { "Name", "PrimaryKeyFieldName" })]
    public async Task<IActionResult> GetDataStores()
    {
      FilterPageAndSortInfo filterPageAndSortInfo = HttpContext.GetFilterPageAndSortInfo();
      IEnumerable<Data.Models.DataStore> dataStores = await dataService.GetDataStores(filterPageAndSortInfo);

      PagedResults<DataStore> pagedResults = new PagedResults<DataStore>()
      {
        List = dataStores.Select(ds => (DataStore)ds).ToList()
      };
      pagedResults.SetFilterPageAndSortInfo(filterPageAndSortInfo);

      return new OkObjectResult(pagedResults);
    }

    /// <summary>
    /// Creates a new data stores
    /// </summary>
    /// <returns>Details for the created data store</returns>
    /// <param name="dataStore">The details of the data store to create</param>
    /// <response code="201">The data store was created sucessfully</response>
    /// <response code="400">A bad request</response>
    [HttpPost("", Name = "CreateDataStore")]
    [ProducesResponseType(typeof(DataStore), 201)]
    [ProducesResponseType(typeof(void), 400)]
    public async Task<IActionResult> CreateDataStore([FromBody] DataStore dataStore)
    {
      if ((dataStore == null) || !ModelState.IsValid)
      {
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoDataStoreDetailsProvided, "No data store details provided")));
      }

      bool success = await dataService.CreateDataStore(dataStore);
      if (!success)
        throw new Exception("Failed to successfully create the data store");

      Uri absoluteSelfUri = routingService.GetAbsoluteRouteUrl(this, "GetDataStoreDetails", new { name = dataStore.Name });
      return new CreatedResult(absoluteSelfUri, dataStore);
    }

    /// <summary>
    /// Returns the details of a particular data store
    /// </summary>
    /// <returns>The details for a particular data store</returns>
    /// <param name="name">The name of the data store to view</param>
    /// <response code="200">The data store details</response>
    /// <response code="400">A bad request</response>
    /// <response code="404">The data store couldn't be found</response>
    [HttpGet("{name}", Name = "GetDataStoreDetails")]
    [ProducesResponseType(typeof(DataStore), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<IActionResult> GetDataStoreDetails(string name)
    {
      if (string.IsNullOrEmpty(name) || !ModelState.IsValid)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoDataStoreNameProvided, "No data store name provided")));

      Data.Models.DataStore dataStore = await dataService.GetDataStore(name);
      if (dataStore == null)
        return NotFound();

      return new OkObjectResult((DataStore) dataStore);
    }
    
    /// <summary>
    /// Deletes the specified data store
    /// </summary>
    /// <returns>No content</returns>
    /// <param name="name">The name of the data store to delete</param>
    /// <response code="204">The data store was deleted successfully</response>
    /// <response code="400">A bad request</response>
    /// <response code="404">The data store couldn't be found</response>
    [HttpDelete("{name}", Name = "DeleteDataStore")]
    [ProducesResponseType(typeof(void), 204)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<IActionResult> DeleteDataStore(string name)
    {
      if (string.IsNullOrEmpty(name) || !ModelState.IsValid)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoDataStoreNameProvided, "No data store name provided")));

      bool success = await dataService.DeleteDataStore(name);
      if (!success)
        return NotFound();

      return NoContent();
    }
  }
}
