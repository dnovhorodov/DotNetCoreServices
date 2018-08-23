using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nancy.Owin;
using Polly;
using Polly.Registry;

namespace DistanceService
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
            PolicyRegistry registry = new PolicyRegistry();
            registry.Add("StandardHttpResilience", GetRetryPolicy());

            services.AddSingleton(Configuration);
            services.AddSingleton<IReadOnlyPolicyRegistry<string>>(registry);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseOwin(b => b.UseNancy(options =>
            {
                options.Bootstrapper = new Bootstrapper(app.ApplicationServices);
            }));
        }

        private IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.BadGateway)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}
