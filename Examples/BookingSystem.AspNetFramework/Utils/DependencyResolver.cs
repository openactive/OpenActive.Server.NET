using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Web.Http.Dependencies;

namespace BookingSystem.AspNetFramework.Utils
{
    public class DependencyResolver : IDependencyResolver
    {
        private readonly IServiceProvider provider;
        private readonly IServiceScope scope;

        public DependencyResolver(ServiceProvider provider) => this.provider = provider;

        internal DependencyResolver(IServiceScope scope)
        {
            this.provider = scope.ServiceProvider;
            this.scope = scope;
        }

        public IDependencyScope BeginScope() =>
            new DependencyResolver(provider.CreateScope());

        public object GetService(Type serviceType) => provider.GetService(serviceType);
        public IEnumerable<object> GetServices(Type type) => provider.GetServices(type);
        public void Dispose() => scope?.Dispose();
    }
}