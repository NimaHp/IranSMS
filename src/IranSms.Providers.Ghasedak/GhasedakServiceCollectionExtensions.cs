using IranSms.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IranSms.Providers.Ghasedak
{
    /// <summary>
    /// Registers the Ghasedak client in a Microsoft DI container.
    /// </summary>
    public static class GhasedakServiceCollectionExtensions
    {
        private const string ClientName = "IranSms.Ghasedak";

        /// <summary>Registers the Ghasedak client (+ HttpClient).</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configures <see cref="GhasedakOptions"/>.</param>
        public static IServiceCollection AddGhasedak(
            this IServiceCollection services,
            Action<GhasedakOptions> configureOptions)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions is null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);
            services.AddHttpClient(ClientName);

            return SmsClientRegistration.RegisterClient<GhasedakClient>(services, sp =>
            {
                var options = sp.GetRequiredService<IOptions<GhasedakOptions>>().Value;
                var apiKey = SmsClientRegistration.Require(options.ApiKey, nameof(GhasedakOptions.ApiKey), "Ghasedak");
                var http = SmsClientRegistration.CreateHttpClient(sp, ClientName, options);
                return new GhasedakClient(apiKey, http);
            });
        }
    }
}