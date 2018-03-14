using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using System;
using IdentityServerHost.Extension;
using IdentityServerHost.Interface;

namespace IdentityServerHost
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public Startup(IHostingEnvironment env)
        {
            var environmentVar = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environmentVar == null)
            {
                environmentVar = env.EnvironmentName;
            }
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentVar}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            // Dependency Injection - Register the IConfigurationRoot instance mapping to our "ConfigurationOptions" class 
            services.Configure<ConfigurationOptions>(Configuration);
            // ---  configure identity server with MONGO Repository for stores, keys, clients and scopes ---
            services.AddIdentityServer()
                //.AddTemporarySigningCredential()
                .AddDeveloperSigningCredential()
                .AddMongoRepository()
                .AddClients()
                .AddIdentityApiResources()
                .AddPersistedGrants()
                .AddTestUsers(Config.GetUsers());

            services.AddAuthentication()
               .AddJwtBearer(jwt =>
               {
                   jwt.Authority = "http://localhost:5000";
                   jwt.RequireHttpsMetadata = false;
                   jwt.Audience = "api1";
               });

            services.AddDistributedRedisCache(option =>
            {
                option.Configuration = "127.0.0.1";
                option.InstanceName = "master";
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
            // --- Configure Classes to ignore Extra Elements (e.g. _Id) when deserializing ---
            ConfigureMongoDriver2IgnoreExtraElements();

            // --- The following will do the initial DB population (If needed / first time) ---
            InitializeDatabase(app);
            app.UseIdentityServer();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }


        private static void InitializeDatabase(IApplicationBuilder app)
        {
            bool createdNewRepository = false;
            var repository = app.ApplicationServices.GetService<IRepository>();

            //  --Client
            if (!repository.CollectionExists<Client>())
            {
                foreach (var client in Config.Clients)
                {
                    repository.Add<Client>(client);
                }
                createdNewRepository = true;
            }

            //  --IdentityResource
            if (!repository.CollectionExists<IdentityResource>())
            {
                foreach (var res in Config.GetIdentityResources())
                {
                    repository.Add<IdentityResource>(res);
                }
                createdNewRepository = true;
            }


            //  --ApiResource
            if (!repository.CollectionExists<ApiResource>())
            {
                foreach (var api in Config.GetApiResources())
                {
                    repository.Add<ApiResource>(api);
                }
                createdNewRepository = true;
            }

            // If it's a new Repository (database), need to restart the website to configure Mongo to ignore Extra Elements.
            if (createdNewRepository)
            {
                var newRepositoryMsg = $"Mongo Repository created/populated! Please restart you website, so Mongo driver will be configured  to ignore Extra Elements.";
                throw new Exception(newRepositoryMsg);
            }
        }

        /// <summary>
        /// Configure Classes to ignore Extra Elements (e.g. _Id) when deserializing
        /// As we are using "IdentityServer4.Models" we cannot add something like "[BsonIgnore]"
        /// </summary>
        private static void ConfigureMongoDriver2IgnoreExtraElements()
        {
            BsonClassMap.RegisterClassMap<Client>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
            BsonClassMap.RegisterClassMap<IdentityResource>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
            BsonClassMap.RegisterClassMap<ApiResource>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
            BsonClassMap.RegisterClassMap<PersistedGrant>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });



        }

    }
}
