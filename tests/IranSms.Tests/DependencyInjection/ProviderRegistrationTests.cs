using System;
using FluentAssertions;
using IranSms.DependencyInjection;
using IranSms.Providers.Ghasedak;
using IranSms.Providers.Kavenegar;
using IranSms.Providers.Melipayamak;
using IranSms.Providers.Mock;
using IranSms.Providers.SmsIr;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IranSms.Tests.DependencyInjection
{
    public class ProviderRegistrationTests
    {
        [Fact]
        public void AddMock_RegistersClient_UnderAllCapabilityInterfaces()
        {
            var services = new ServiceCollection();

            var result = services.AddMock(options => options.ProviderName = "Demo");
            var provider = services.BuildServiceProvider();

            result.Should().BeSameAs(services);
            provider.GetRequiredService<ISmsClient>().Should().BeAssignableTo<MockSmsClient>();
            provider.GetRequiredService<MockSmsClient>().ProviderName.Should().Be("Demo");
        }

        [Fact]
        public void AddMock_ResolvesClient_AsEachCapabilityInterface()
        {
            var provider = new ServiceCollection()
                .AddMock()
                .BuildServiceProvider();

            provider.GetRequiredService<ISmsClient>().Should().BeAssignableTo<ISmsBulkSender>();
            provider.GetRequiredService<ISmsClient>().Should().BeAssignableTo<ISmsOtpSender>();
            provider.GetRequiredService<ISmsClient>().Should().BeAssignableTo<ISmsDeliveryReporter>();
        }

        [Theory]
        [InlineData(typeof(KavenegarClient), "Kavenegar", "api-key")]
        [InlineData(typeof(SmsIrClient), "SmsIr", "api-key")]
        [InlineData(typeof(GhasedakClient), "Ghasedak", "api-key")]
        public void AddProvider_ResolvesTypedClient(Type clientType, string provider, string apiKey)
        {
            var services = new ServiceCollection();
            switch (clientType.Name)
            {
                case "KavenegarClient":
                    services.AddKavenegar(o => o.ApiKey = apiKey);
                    break;
                case "SmsIrClient":
                    services.AddSmsIr(o => o.ApiKey = apiKey);
                    break;
                case "GhasedakClient":
                    services.AddGhasedak(o => o.ApiKey = apiKey);
                    break;
            }

            var providerContainer = services.BuildServiceProvider();
            var client = providerContainer.GetRequiredService<ISmsClient>();

            client.Should().BeOfType(clientType);
            client.ProviderName.Should().Be(provider);
            client.Should().BeAssignableTo<ISmsBulkSender>();
            client.Should().BeAssignableTo<ISmsOtpSender>();
            client.Should().BeAssignableTo<ISmsDeliveryReporter>();
        }

        [Fact]
        public void AddMelipayamak_RequiresUsernameAndPassword()
        {
            var provider = new ServiceCollection()
                .AddMelipayamak(o =>
                {
                    o.Username = "user";
                    o.Password = "pass";
                })
                .BuildServiceProvider();

            provider.GetRequiredService<ISmsClient>().Should().BeOfType<MelipayamakClient>();
        }

        [Fact]
        public void AddProvider_ThrowsWhenCredentialsMissing()
        {
            var provider = new ServiceCollection()
                .AddKavenegar(_ => { })
                .BuildServiceProvider();

            Assert.Throws<IranSmsException>(() => provider.GetRequiredService<ISmsClient>());
        }

        [Fact]
        public void AddIranSms_RegistersCustomImplementation_WithoutProviderPackage()
        {
            var services = new ServiceCollection();

            var result = services.AddIranSms<FakeSmsClient>(new FakeSmsClient());
            var provider = services.BuildServiceProvider();

            result.Should().BeSameAs(services);
            provider.GetRequiredService<ISmsClient>().Should().BeOfType<FakeSmsClient>();
        }

        [Fact]
        public void AddIranSms_RejectsNullInstance()
        {
            var services = new ServiceCollection();
            FakeSmsClient? instance = null;

            var act = () => services.AddIranSms<FakeSmsClient>(instance!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("instance");
        }

        private sealed class FakeSmsClient : ISmsClient
        {
            public string ProviderName => "Fake";

            public SmsCapabilities Capabilities => SmsCapabilities.Send;

            public Task<SmsSendResult> SendAsync(
                string recipient,
                string message,
                string? senderLine = null,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(new SmsSendResult(recipient));
        }
    }
}