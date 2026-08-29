using Microsoft.Extensions.DependencyInjection;

namespace IranSms.DependencyInjection
{
    /// <summary>
    /// Consumer-owned DI registration for IranSms. The DI package is provider-agnostic:
    /// it never references a provider package, and no provider package references the DI
    /// package. A consumer builds a concrete client instance (owning any
    /// <see cref="System.Net.Http.HttpClient"/>/message handler and setting its options)
    /// and registers that instance here under every capability it actually implements.
    /// </summary>
    public static class IranSmsServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a client instance as a singleton, exposed under <see cref="ISmsClient"/>
        /// plus every Sms capability interface the concrete instance implements
        /// (<see cref="ISmsBulkSender"/>, <see cref="ISmsOtpSender"/>,
        /// <see cref="ISmsDeliveryReporter"/>). Resolution is capability-aware: an interface
        /// the instance does not implement is never registered, so resolving it throws
        /// <see cref="InvalidOperationException"/> rather than <see cref="InvalidCastException"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="client">Pre-built client instance (already configured with credentials
        /// and its own transport).</param>
        public static IServiceCollection AddIranSms(this IServiceCollection services, ISmsClient client)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            if (client is null)
                throw new ArgumentNullException(nameof(client));

            services.AddSingleton(client.GetType(), client);
            services.AddSingleton<ISmsClient>(client);

            if (client is ISmsBulkSender bulkSender)
                services.AddSingleton<ISmsBulkSender>(bulkSender);

            if (client is ISmsOtpSender otpSender)
                services.AddSingleton<ISmsOtpSender>(otpSender);

            if (client is ISmsDeliveryReporter deliveryReporter)
                services.AddSingleton<ISmsDeliveryReporter>(deliveryReporter);

            return services;
        }
    }
}
