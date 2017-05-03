using System;
using System.Threading.Tasks;
using ApiPager.Core;
using ApiPager.Core.Models;
using Apteco.PullMarketing.Models.DataStores;
using Microsoft.AspNetCore.Mvc;

namespace Apteco.PullMarketing.Controllers
{
  /// <summary>
  /// An endpoint within the API to manipulate data stores
  /// </summary>
  [Route("api/[controller]")]
  public class DataStoresController : Controller
  {
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
      return new OkObjectResult(null);
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
        return BadRequest();
      }

      return new CreatedResult((Uri)null, null);
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
      return new OkObjectResult(null);
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
      return NoContent();
    }
  }
}
