﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using RestEase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProCRM.Core.Rest
{
    public static class Extensions
    {
        public static void RegisterServiceForwarder<T>(this IServiceCollection services, string serviceName)
          where T : class
        {
            var clientName = typeof(T).ToString();
            var options = ConfigureOptions(services);

            services.AddHttpClient(clientName, client =>
            {
                var service = options.Services
                    .SingleOrDefault(s => s.Name.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));

                if (service == null)
                    throw new InvalidOperationException($"RestEase service '{serviceName}' was not found");

                client.BaseAddress = new UriBuilder
                {
                    Scheme = service.Scheme,
                    Host = service.Host,
                    Port = service.Port
                }.Uri;

            });

            ConfigureForwarder<T>(services, clientName);
        }

        private static void ConfigureForwarder<T>(IServiceCollection services, string clientName) where T : class
        {
            services.AddTransient<T>(c => new RestClient(c.GetService<IHttpClientFactory>().CreateClient(clientName))
            {
                JsonSerializerSettings = new Newtonsoft.Json.JsonSerializerSettings()
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    }
                }
            }.For<T>());
        }

        private static RestEaseConfiguration ConfigureOptions(IServiceCollection services)
        {
            IConfiguration configuration;
            using (var serviceProvider = services.BuildServiceProvider())
            {
                configuration = serviceProvider.GetService<IConfiguration>();
            }

            RestEaseConfiguration restEaseConfiguration = new RestEaseConfiguration();

            IConfigurationSection configurationSection = configuration.GetSection("restEase");
            configurationSection.Bind(restEaseConfiguration);

            services.Configure<RestEaseConfiguration>(configuration.GetSection("restEase"));

            return restEaseConfiguration;
        }
    }
}
