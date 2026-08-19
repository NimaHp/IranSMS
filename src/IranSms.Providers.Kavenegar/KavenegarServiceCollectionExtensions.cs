using IranSms.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IranSms.Providers.Kavenegar
{
    /// <summary>
    /// Registers the Kavenegar client in a Microsoft DI container.
    /// </summary>
    public static class KavenegarServiceCollectionExtensions
    {
        private const string ClientName = "IranSms.Kavenegar";

        /// <summary>Registers the Kavenegar client (+ HttpClient).</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configures <see cref="KavenegarOptions"/>.</param>
        public static IServiceCollection AddKavenegar(
            this IServiceCollection services,
            Action<KavenegarOptions> configureOptions)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions is null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);
            services.AddHttpClient(ClientName);

            return SmsClientRegistration.RegisterClient<KavenegarClient>(services, sp =>
            {
                var options = sp.GetRequiredService<IOptions<KavenegarOptions>>().Value;
                var apiKey = SmsClientRegistration.Require(options.ApiKey, nameof(KavenegarOptions.ApiKey), "Kavenegar");
                var http = SmsClientRegistration.CreateHttpClient(sp, ClientName, options);
                return new KavenegarClient(apiKey, http);
            });
        }
    }
}