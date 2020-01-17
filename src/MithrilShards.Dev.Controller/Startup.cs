using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace MithrilShards.Dev.Controller
{
   public class Startup
   {
      public IConfiguration Configuration { get; }

      public Startup(IConfiguration configuration)
      {
         this.Configuration = configuration;
      }

      // This method gets called by the runtime. Use this method to add services to the container.
      public void ConfigureServices(IServiceCollection services)
      {
         services.AddControllers();
         services.AddSwaggerGen(setup => setup.SwaggerDoc("v1", new OpenApiInfo { Title = "Dev Controller", Version = "v1" }));
      }

      // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
      public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
      {
         app
            //.UseMvc()
            .UseRouting()
            .UseSwagger()
            .UseSwaggerUI(setup => setup.SwaggerEndpoint("/swagger/v1/swagger.json", "Dev Controller"))
            .UseEndpoints(endpoints =>
            {
               endpoints.MapControllers();
            });
      }
   }
}
