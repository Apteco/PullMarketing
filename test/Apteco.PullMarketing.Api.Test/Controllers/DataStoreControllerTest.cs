using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ApiPager.Core;
using ApiPager.Core.Models;
using Apteco.PullMarketing.Api.Test.TestHelpers;
using Apteco.PullMarketing.Api.Controllers;
using Apteco.PullMarketing.Api.Models;
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
    private Mock<IDataService> dataService;
    private Mock<IRoutingService> routingService;
    private DataStoresController controller;

    [SetUp]
    public void SetUp()
    {
      dataService = new Mock<IDataService>();
      routingService = new Mock<IRoutingService>();

      controller = new DataStoresController(dataService.Object, routingService.Object);

      routingService
        .Setup(s => s.GetAbsoluteRouteUrl(controller, "GetDataStoreDetails", TestUtilities.IsAnonymousTypeWithProperties("name")))
        .Returns<DataStoresController, string, object>((c, r, p) => new Uri("http://example.com/api/DataStores/" + TestUtilities.GetPropertyValue(p, "name")));
    }

    #region GetDataStores Tests
    [Test]
    public async Task TestGetEmptyDataStores()
    {
      dataService
        .Setup(s => s.GetDataStores(It.IsAny<FilterPageAndSortInfo>()))
        .Returns(Task.FromResult((IEnumerable<DataStore>)new List<DataStore>()));

      var result = await controller.GetDataStores();

      Assert.IsInstanceOf<OkObjectResult>(result);
      PagedResults<Models.DataStores.DataStore> dataStoreResults = ((OkObjectResult)result).Value as PagedResults<Models.DataStores.DataStore>;

      Assert.IsNotNull(dataStoreResults);
      Assert.AreEqual(0, dataStoreResults.List.Count);
    }

    [Test]
    public async Task TestGetSingleDataStore()
    {
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

      var result = await controller.GetDataStores();

      Assert.IsInstanceOf<OkObjectResult>(result);
      PagedResults<Models.DataStores.DataStore> dataStoreResults = ((OkObjectResult)result).Value as PagedResults<Models.DataStores.DataStore>;

      Assert.IsNotNull(dataStoreResults);
      Assert.AreEqual(1, dataStoreResults.List.Count);
      Assert.AreEqual("People", dataStoreResults.List[0].Name);
      Assert.AreEqual("PersonUrn", dataStoreResults.List[0].PrimaryKeyFieldName);
    }

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
    public async Task TestGetMultipleDataStoresPagedViaClient()
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

        var response = await client.GetAsync($"/api/DataStores?offset=1&count=2");

        TestUtilities.AssertResponseHasStatusCodeAndBody(HttpStatusCode.OK, "{\"offset\":1,\"count\":2,\"totalCount\":null,\"list\":[{\"name\":\"Bookings\",\"primaryKeyFieldName\":\"BookingUrn\"},{\"name\":\"Policies\",\"primaryKeyFieldName\":\"PolicyUrn\"}]}", response);
      }
    }
    #endregion

    #region CreateDataStore Tests
    [Test]
    public async Task TestCreateDataStoreBadInput()
    {
      var result = await controller.CreateDataStore(null);

      Assert.IsInstanceOf<BadRequestObjectResult>(result);
      ErrorMessages errorMessages = ((BadRequestObjectResult)result).Value as ErrorMessages;

      Assert.IsNotNull(errorMessages);
      Assert.AreEqual(1, errorMessages.Errors.Count);
      Assert.AreEqual((int)ErrorMessageCodes.NoDataStoreDetailsProvided, errorMessages.Errors[0].Code);
      Assert.AreEqual("No data store details provided", errorMessages.Errors[0].Message);
    }

    [Test]
    public async Task TestCreateDataStore()
    {
      dataService
        .Setup(s => s.CreateDataStore(It.Is<DataStore>(d => d.Name == "People" && d.PrimaryKeyFieldName == "PersonUrn")))
        .Returns(Task.FromResult(true));

      var result = await controller.CreateDataStore(new Models.DataStores.DataStore()
      {
        Name = "People",
        PrimaryKeyFieldName = "PersonUrn"
      });

      Assert.IsInstanceOf<CreatedResult>(result);
      Models.DataStores.DataStore dataStore = ((CreatedResult)result).Value as Models.DataStores.DataStore;

      Assert.IsNotNull(dataStore);
      Assert.AreEqual("People", dataStore.Name);
      Assert.AreEqual("PersonUrn", dataStore.PrimaryKeyFieldName);
      Assert.AreEqual("http://example.com/api/DataStores/People", ((CreatedResult)result).Location);
    }

    [Test]
    public void TestCreateDataStoreFails()
    {
      dataService
        .Setup(s => s.CreateDataStore(It.Is<DataStore>(d => d.Name == "People" && d.PrimaryKeyFieldName == "PersonUrn")))
        .Returns(Task.FromResult(false));

      Assert.ThrowsAsync<Exception>(() => controller.CreateDataStore(new Models.DataStores.DataStore()
      {
        Name = "People",
        PrimaryKeyFieldName = "PersonUrn"
      }), "Failed to successfully create the data store");
    }
    #endregion

    #region GetDataStore Tests
    [Test]
    public async Task TestGetDataStoreDetails()
    {
      dataService
        .Setup(s => s.GetDataStore("People"))
        .Returns(Task.FromResult(new DataStore()
          {
            Name = "People",
            PrimaryKeyFieldName = "PersonUrn"
          }));

      var result = await controller.GetDataStoreDetails("People");

      Assert.IsInstanceOf<OkObjectResult>(result);
      Models.DataStores.DataStore dataStore = ((OkObjectResult)result).Value as Models.DataStores.DataStore;

      Assert.AreEqual("People", dataStore.Name);
      Assert.AreEqual("PersonUrn", dataStore.PrimaryKeyFieldName);
    }

    [Test]
    public async Task TestGetUnknownDataStoreDetails()
    {
      var result = await controller.GetDataStoreDetails("OtherDataStore");

      Assert.IsInstanceOf<NotFoundResult>(result);
    }

    [Test]
    public async Task TestGetNullDataStoreDetails()
    {
      var result = await controller.GetDataStoreDetails(null);

      Assert.IsInstanceOf<BadRequestObjectResult>(result);
      ErrorMessages errorMessages = ((BadRequestObjectResult)result).Value as ErrorMessages;

      Assert.IsNotNull(errorMessages);
      Assert.AreEqual(1, errorMessages.Errors.Count);
      Assert.AreEqual((int)ErrorMessageCodes.NoDataStoreNameProvided, errorMessages.Errors[0].Code);
      Assert.AreEqual("No data store name provided", errorMessages.Errors[0].Message);
    }

    #endregion

    #region DeleteDataStore Tests
    [Test]
    public async Task TestDeleteDataStore()
    {
      dataService
        .Setup(s => s.DeleteDataStore("People"))
        .Returns(Task.FromResult(true));

      var result = await controller.DeleteDataStore("People");

      Assert.IsInstanceOf<NoContentResult>(result);
    }

    [Test]
    public async Task TestDeleteUnknownDataStore()
    {
      var result = await controller.DeleteDataStore("OtherDataStore");

      Assert.IsInstanceOf<NotFoundResult>(result);
    }

    [Test]
    public async Task TestDeleteNullDataStore()
    {
      var result = await controller.DeleteDataStore(null);

      Assert.IsInstanceOf<BadRequestObjectResult>(result);
      ErrorMessages errorMessages = ((BadRequestObjectResult)result).Value as ErrorMessages;

      Assert.IsNotNull(errorMessages);
      Assert.AreEqual(1, errorMessages.Errors.Count);
      Assert.AreEqual((int)ErrorMessageCodes.NoDataStoreNameProvided, errorMessages.Errors[0].Code);
      Assert.AreEqual("No data store name provided", errorMessages.Errors[0].Message);
    }

    #endregion
  }
}
