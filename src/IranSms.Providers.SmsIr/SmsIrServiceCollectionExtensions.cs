using IranSms.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IranSms.Providers.SmsIr
{
    /// <summary>
    /// Registers the SMS.ir client in a Microsoft DI container.
    /// </summary>
    public static class SmsIrServiceCollectionExtensions
    {
        private const string ClientName = "IranSms.SmsIr";

        /// <summary>Registers the SMS.ir client (+ HttpClient).</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configures <see cref="SmsIrOptions"/>.</param>
        public static IServiceCollection AddSmsIr(
            this IServiceCollection services,
            Action<SmsIrOptions> configureOptions)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions is null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);
            services.AddHttpClient(ClientName);

            return SmsClientRegistration.RegisterClient<SmsIrClient>(services, sp =>
            {
                var options = sp.GetRequiredService<IOptions<SmsIrOptions>>().Value;
                var apiKey = SmsClientRegistration.Require(options.ApiKey, nameof(SmsIrOptions.ApiKey), "SMS.ir");
                var http = SmsClientRegistration.CreateHttpClient(sp, ClientName, options);
                return new SmsIrClient(apiKey, http);
            });
        }
    }
}