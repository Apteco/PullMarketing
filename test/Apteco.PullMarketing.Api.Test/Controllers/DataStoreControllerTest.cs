using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ApiPager.Core;
using ApiPager.Core.Models;
using Apteco.PullMarketing.Api.Test.TestHelpers;
using Apteco.PullMarketing.Api.Controllers;
using Apteco.PullMarketing.Data;
using Apteco.PullMarketing.Data.Models;
using Apteco.PullMarketing.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Moq;
using NUnit.Framework;

namespace Apteco.PullMarketing.Api.Test.Controllers
{
  [TestFixture]
  public class DataStoreControllerTest
  {
    #region DataStores Tests
    [Test]
    public async Task TestGetEmptyDataStoresViaClient()
    {
      TestServerBuilder testServerBuilder = new TestServerBuilder();

      using (TestServer server = testServerBuilder.Build())
      {
        server.GetMockDataService()
          .Setup(s => s.GetDataStores(It.IsAny<FilterPageAndSortInfo>()))
          .Returns(Task.FromResult((IEnumerable<DataStore>) new List<DataStore>()));

        var client = server.CreateClient();

        var response = await client.GetAsync($"/api/DataStores");

        TestUtilities.AssertResponseHasStatusCodeAndBody(HttpStatusCode.OK, "{\"offset\":0,\"count\":10,\"totalCount\":null,\"list\":[]}", response);
      }
    }

    [Test]
    public async Task TestGetSingleDataStoreViaClient()
    {
      TestServerBuilder testServerBuilder = new TestServerBuilder();

      using (TestServer server = testServerBuilder.Build())
      {
        server.GetMockDataService()
          .Setup(s => s.GetDataStores(It.IsAny<FilterPageAndSortInfo>()))
          .Returns(Task.FromResult((IEnumerable<DataStore>)new List<DataStore>()
          {
            new DataStore()
            {
              Name = "People",
              PrimaryKeyFieldName = "PersonUrn"
            }
          }));

        var client = server.CreateClient();

        var response = await client.GetAsync($"/api/DataStores");

        TestUtilities.AssertResponseHasStatusCodeAndBody(HttpStatusCode.OK, "{\"offset\":0,\"count\":10,\"totalCount\":null,\"list\":[{\"name\":\"People\",\"primaryKeyFieldName\":\"PersonUrn\"}]}", response);
      }
    }

    [Test]
    public async Task TestGetMultipleDataStoresViaClient()
    {
      TestServerBuilder testServerBuilder = new TestServerBuilder();

      using (TestServer server = testServerBuilder.Build())
      {
        server.GetMockDataService()
          .Setup(s => s.GetDataStores(It.IsAny<FilterPageAndSortInfo>()))
          .Returns(Task.FromResult((IEnumerable<DataStore>)new List<DataStore>()
          {
            new DataStore()
            {
              Name = "People",
              PrimaryKeyFieldName = "PersonUrn"
            },
            new DataStore()
            {
              Name = "Bookings",
              PrimaryKeyFieldName = "BookingUrn"
            },
            new DataStore()
            {
              Name = "Policies",
              PrimaryKeyFieldName = "PolicyUrn"
            }
          }));

        var client = server.CreateClient();

        var response = await client.GetAsync($"/api/DataStores");

        TestUtilities.AssertResponseHasStatusCodeAndBody(HttpStatusCode.OK, "{\"offset\":0,\"count\":10,\"totalCount\":null,\"list\":[{\"name\":\"People\",\"primaryKeyFieldName\":\"PersonUrn\"},{\"name\":\"Bookings\",\"primaryKeyFieldName\":\"BookingUrn\"},{\"name\":\"Policies\",\"primaryKeyFieldName\":\"PolicyUrn\"}]}", response);
      }
    }

    [Test]
    public async Task TestGetSingleDataStore()
    {
      Mock<IDataService> dataService = new Mock<IDataService>();
      Mock<IRoutingService> routingService = new Mock<IRoutingService>();

      dataService
        .Setup(s => s.GetDataStores(It.IsAny<FilterPageAndSortInfo>()))
        .Returns(Task.FromResult((IEnumerable<DataStore>)new List<DataStore>()
        {
          new DataStore()
          {
            Name = "People",
            PrimaryKeyFieldName = "PersonUrn"
          }
        }));

      DataStoresController controller = new DataStoresController(dataService.Object, routingService.Object);
      var result = await controller.GetDataStores();

      Assert.IsInstanceOf<OkObjectResult>(result);
      PagedResults<Models.DataStores.DataStore> dataStoreResults = ((OkObjectResult)result).Value as PagedResults<Models.DataStores.DataStore>;

      Assert.IsNotNull(dataStoreResults);
      Assert.AreEqual(1, dataStoreResults.List.Count);
      Assert.AreEqual("People", dataStoreResults.List[0].Name);
      Assert.AreEqual("PersonUrn", dataStoreResults.List[0].PrimaryKeyFieldName);
    }
    #endregion
  }
}
