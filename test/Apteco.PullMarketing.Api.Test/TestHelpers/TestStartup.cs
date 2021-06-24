using Apteco.PullMarketing.Data;
using Apteco.PullMarketing.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Apteco.PullMarketing.Api.Test.TestHelpers
{
  public class TestStartup : Startup
  {
    #region public properties
    public Mock<IDataService> MockDataService { get; }
    public Mock<IRoutingService> MockRoutingService { get; }
    #endregion

    #region public constructor
    public TestStartup(TestConfiguration configuration)
      : base(configuration.Configuration)
    {
      MockDataService = new Mock<IDataService>();
      MockRoutingService = new Mock<IRoutingService>();
    }
    #endregion

    #region protected methods
    protected override void CreateServices(IServiceCollection services)
    {
      services.AddSingleton<IDataService>(MockDataService.Object);
      services.AddSingleton<IRoutingService>(MockRoutingService.Object);
    }
    #endregion
  }
}
