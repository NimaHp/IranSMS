using IranSms.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IranSms.Providers.Melipayamak
{
    /// <summary>
    /// Registers the Melipayamak client in a Microsoft DI container.
    /// </summary>
    public static class MelipayamakServiceCollectionExtensions
    {
        private const string ClientName = "IranSms.Melipayamak";

        /// <summary>Registers the Melipayamak client (+ HttpClient).</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configures <see cref="MelipayamakOptions"/>.</param>
        public static IServiceCollection AddMelipayamak(
            this IServiceCollection services,
            Action<MelipayamakOptions> configureOptions)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions is null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);
            services.AddHttpClient(ClientName);

            return SmsClientRegistration.RegisterClient<MelipayamakClient>(services, sp =>
            {
                var options = sp.GetRequiredService<IOptions<MelipayamakOptions>>().Value;
                var username = SmsClientRegistration.Require(options.Username, nameof(MelipayamakOptions.Username), "Melipayamak");
                var password = SmsClientRegistration.Require(options.Password, nameof(MelipayamakOptions.Password), "Melipayamak");
                var http = SmsClientRegistration.CreateHttpClient(sp, ClientName, options);
                return new MelipayamakClient(username, password, http);
            });
        }
    }
}