using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using WebApplicationExternalAuth.Configurations;
using WebApplicationExternalAuth.Model;

namespace WebApplicationExternalAuth
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<IdentityAppDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("MainConnection")), optionsLifetime: ServiceLifetime.Transient);
            services.AddControllers();
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<IdentityAppDbContext>()
                .AddDefaultTokenProviders();
            services.AddIdentityServer(options => options.IssuerUri = "localhost")
                .AddInMemoryApiResources(ClientStore.GetApiResources())
                .AddInMemoryApiScopes(ClientStore.GetApiScopes())
                .AddInMemoryIdentityResources(ClientStore.GetIdentityResources())
                .AddInMemoryClients(ClientStore.GetClients())
                .AddDeveloperSigningCredential(false)
                .AddAspNetIdentity<ApplicationUser>();
            
            services.AddAuthentication()
                .AddGoogle("Google", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    options.ClientId = "868307066027-q9m7b35h8e5opqo9j8b1ht09f86lsk9s.apps.googleusercontent.com";
                    options.ClientSecret = "NuFJ-rcKbGOHbbsZ9vYDlowe";
                });
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "IdentityServer4_implementation", Version = "v1"});
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(
                    c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "IdentityServer4_implementation v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseIdentityServer();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}