using Microsoft.Extensions.DependencyInjection;

namespace IranSms.DependencyInjection
{
    /// <summary>
    /// Shared registration infrastructure for provider-author packages.
    /// Each provider's <c>Add&lt;Provider&gt;</c> extension registers its concrete
    /// client as a singleton and forwards it to <see cref="ISmsClient"/> and every
    /// capability interface it implements (<see cref="ISmsBulkSender"/>,
    /// <see cref="ISmsOtpSender"/>, <see cref="ISmsDeliveryReporter"/>).
    /// </summary>
    public static class SmsClientRegistration
    {
        /// <summary>
        /// Registers a client factory under the concrete type and forwards the
        /// created instance to every capability interface the concrete type
        /// implements.
        /// </summary>
        /// <typeparam name="TClient">Concrete provider client.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="factory">Resolves a client instance from the provider.</param>
        public static IServiceCollection RegisterClient<TClient>(
            IServiceCollection services,
            Func<IServiceProvider, TClient> factory)
            where TClient : class, ISmsClient
        {
            services.AddSingleton<TClient>(factory);
            services.AddSingleton<ISmsClient>(sp => sp.GetRequiredService<TClient>());
            services.AddSingleton<ISmsBulkSender>(sp => (ISmsBulkSender)sp.GetRequiredService<TClient>());
            services.AddSingleton<ISmsOtpSender>(sp => (ISmsOtpSender)sp.GetRequiredService<TClient>());
            services.AddSingleton<ISmsDeliveryReporter>(sp => (ISmsDeliveryReporter)sp.GetRequiredService<TClient>());
            return services;
        }

        /// <summary>
        /// Creates a named <see cref="System.Net.Http.HttpClient"/> from
        /// <c>IHttpClientFactory</c> and applies the client <see cref="SmsClientOptions.Timeout"/>.
        /// </summary>
        /// <param name="sp">The service provider.</param>
        /// <param name="name">The configured HttpClient name.</param>
        /// <param name="options">The client options (may carry a Timeout).</param>
        public static System.Net.Http.HttpClient CreateHttpClient(
            IServiceProvider sp, string name, SmsClientOptions options)
        {
            var http = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(name);
            if (options.Timeout is not null)
                http.Timeout = options.Timeout.Value;
            return http;
        }

        /// <summary>
        /// Validates that a required credential is non-empty or throws
        /// <see cref="IranSmsException"/>.
        /// </summary>
        /// <param name="value">The candidate credential value.</param>
        /// <param name="name">Options member name (for the message).</param>
        /// <param name="provider">Display provider name (for the message).</param>
        public static string Require(string? value, string name, string provider)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new IranSmsException(
                    $"{provider} requires a non-empty {name}; configure it in the Add{provider} options.");
            return value!;
        }
    }
}