using IranSms.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IranSms.Providers.Mock
{
    /// <summary>
    /// Registers the Mock client in a Microsoft DI container. No network calls;
    /// useful for tests and demos.
    /// </summary>
    public static class MockSmsServiceCollectionExtensions
    {
        /// <summary>Registers the Mock client.</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configures <see cref="MockSmsOptions"/>.</param>
        public static IServiceCollection AddMock(
            this IServiceCollection services,
            Action<MockSmsOptions>? configureOptions = null)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            services.Configure<MockSmsOptions>(_ => { });
            if (configureOptions is not null)
                services.Configure(configureOptions);

            return SmsClientRegistration.RegisterClient<MockSmsClient>(services, sp =>
            {
                var options = sp.GetRequiredService<IOptions<MockSmsOptions>>().Value;
                return new MockSmsClient(options.ProviderName);
            });
        }
    }
}