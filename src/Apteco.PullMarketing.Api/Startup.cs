using System;
using System.IO;
using Apteco.PullMarketing.Data;
using Apteco.PullMarketing.Data.Dynamo;
using Apteco.PullMarketing.Data.Mongo;
using Apteco.PullMarketing.Api.Services;
using Apteco.PullMarketing.Api.Swagger;
using Apteco.PullMarketing.Data.MSSQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.PlatformAbstractions;

namespace Apteco.PullMarketing.Api
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      CreateServices(services);
      services.AddControllers();
      services.AddSwaggerGen(c =>
        {
          c.SwaggerDoc("v1", new OpenApiInfo
            { Version = "v1",
              Title = "Apteco Pull Marketing",
              Description = "An API to allow access to Apteco Pull Marketing resources",
              Contact = new OpenApiContact { Name = "Apteco Ltd", Email = "support@apteco.com", Url = new Uri("http://www.apteco.com") },
          });

          var basePath = PlatformServices.Default.Application.ApplicationBasePath;

          c.IncludeXmlComments(Path.Combine(basePath, "Apteco.PullMarketing.Api.xml"));
          c.OperationFilter<OperationNameFilter>();
          c.OperationFilter<AddFilterPageAndSortOperationFilter>();
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseSwagger();

      app.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "Pull Marketing API v1"); });

      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }

    protected virtual void CreateServices(IServiceCollection services)
    {
      var dynamoConnection = Configuration.GetSection("DynamoConnection");
      var mongoConnection = Configuration.GetSection("MongoConnection");
      var msSQLConnection = Configuration.GetSection("MSSQLConnection");

      if (dynamoConnection.Exists())
      {
        services.Configure<DynamoConnectionSettings>(dynamoConnection);

        services.AddSingleton<IDynamoConnectionSettings, DynamoOptionConnectionSettings>();
        services.AddSingleton<IDataService, DynamoService>();
      }
      else if (mongoConnection.Exists())
      {
        services.Configure<MongoConnectionSettings>(mongoConnection);

        services.AddSingleton<IMongoConnectionSettings, MongoOptionConnectionSettings>();
        services.AddSingleton<IDataService, MongoService>();
      }
      else if (msSQLConnection.Exists())
      {
        services.Configure<MSSQLConnectionSettings>(msSQLConnection);

        services.AddSingleton<IMSSQLConnectionSettings, MSSQLOptionConnectionSettings>();
        services.AddSingleton<IDataService, MSSQLService>();
      }
      else
      {
        services.AddSingleton<IDataService, NullDataService>();
      }

      services.AddSingleton<IRoutingService, RoutingService>();
    }
  }
}
