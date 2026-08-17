using Microsoft.Extensions.DependencyInjection;

namespace IranSms.DependencyInjection
{
    /// <summary>
    /// Registration helpers for the optional DI package. Providers register
    /// their concrete client factory here; consumers resolve <see cref="ISmsClient"/>.
    /// </summary>
    public static class IranSmsServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a provider client instance as a singleton <see cref="ISmsClient"/>.
        /// </summary>
        /// <typeparam name="TClient">Concrete provider client (implements ISmsClient).</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="instance">Pre-built client instance (e.g. with API key configured).</param>
        public static IServiceCollection AddIranSms<TClient>(this IServiceCollection services, TClient instance)
            where TClient : class, ISmsClient
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            services.AddSingleton<ISmsClient>(instance);
            return services;
        }
    }
}