using ApiPager.AspNetCore.Swashbuckle;
using Apteco.PullMarketing.Data;
using Apteco.PullMarketing.Data.Dynamo;
using Apteco.PullMarketing.Data.Mongo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Apteco.PullMarketing.ModelBinding;
using Apteco.PullMarketing.Services;
using Apteco.PullMarketing.Swagger;
using Swashbuckle.AspNetCore.Swagger;

namespace Apteco.PullMarketing
{
  public class Startup
  {
    private readonly ILoggerFactory loggerFactory;

    public IConfigurationRoot Configuration { get; }

    public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
      : this(env, loggerFactory, BuildConfiguration(env))
    {
    }

    public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory, IConfigurationRoot configuration)
    {
      this.loggerFactory = loggerFactory;
      Configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddMvc(properties =>
      {
        properties.ModelBinderProviders.Insert(0, new JsonModelBinderProvider());
      });

      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new Info()
        {
          Version = "v1",
          Title = "FastStats Pull Marketing Module",
          Description = "An API to allow creating and fast retreval of information about records processed by FastStats",
          TermsOfService = "None",
          Contact = new Contact { Name = "Apteco Ltd", Email = "support@apteco.com", Url = "http://www.apteco.com" },
          License = new License { Name = "Apache 2.0 Licence", Url = "https://github.com/Apteco/PullMarketing/blob/master/LICENSE" }
        });

        var basePath = PlatformServices.Default.Application.ApplicationBasePath;

        //Set the comments path for the swagger json and ui.
        c.IncludeXmlComments(basePath + "\\Apteco.PullMarketing.Api.xml");
        c.DescribeAllEnumsAsStrings();
        c.DescribeStringEnumsInCamelCase();
        c.OperationFilter<FixFormInParameterFilter>();
        c.OperationFilter<RemoveTextPlainContentTypeFilter>();
        c.OperationFilter<OperationNameFilter>();
        c.OperationFilter<AddFilterPageAndSortOperationFilter>();
        c.OperationFilter<MultiPartFormDataWithFileFilter>();
      });

      services.AddOptions();
      services.AddLogging();

      CreateServices(services);
      services.AddSingleton(this);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
    {
      loggerFactory.AddConsole(Configuration.GetSection("Logging"));
      loggerFactory.AddDebug();

      app.UseMvc();

      app.UseSwagger();
      app.UseSwaggerUI(c =>
        {
          c.SwaggerEndpoint("v1/swagger.json", "Pull Marketing API v1");
        });
    }

    protected virtual void CreateServices(IServiceCollection services)
    {
      var dynamoConnection = Configuration.GetSection("DynamoConnection");
      var mongoConnection = Configuration.GetSection("MongoConnection");
      if (dynamoConnection != null)
      {
        services.Configure<DynamoConnectionSettings>(dynamoConnection);

        services.AddSingleton<IDynamoConnectionSettings, DynamoOptionConnectionSettings>();
        services.AddSingleton<IDataService, DynamoService>();
      }
      else if (mongoConnection != null)
      {
        services.Configure<MongoConnectionSettings>(mongoConnection);

        services.AddSingleton<IMongoConnectionSettings, MongoOptionConnectionSettings>();
        services.AddSingleton<IDataService, MongoService>();
      }
      else
      {
        services.AddSingleton<IDataService, NullDataService>();
      }

      services.AddSingleton<IRoutingService, RoutingService>();
    }
    
    private static IConfigurationRoot BuildConfiguration(IHostingEnvironment env)
    {
      var builder = new ConfigurationBuilder()
        .SetBasePath(env.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables();

      if (env.IsDevelopment())
      {
        builder.AddUserSecrets<Startup>();
      }
      return builder.Build();
    }
  }
}
