using FluentAssertions;
using IranSms.DependencyInjection;
using IranSms.Providers.Kavenegar;
using IranSms.Providers.Mock;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IranSms.Tests.DependencyInjection
{
    public class ProviderRegistrationTests
    {
        [Fact]
        public void AddIranSms_RegistersInstance_UnderEveryImplementedCapabilityInterface()
        {
            var mock = new MockSmsClient("Demo");
            var provider = new ServiceCollection()
                .AddIranSms(mock)
                .BuildServiceProvider();

            provider.GetRequiredService<ISmsClient>().Should().BeSameAs(mock);
            provider.GetRequiredService<MockSmsClient>().ProviderName.Should().Be("Demo");
            provider.GetRequiredService<ISmsBulkSender>().Should().BeSameAs(mock);
            provider.GetRequiredService<ISmsOtpSender>().Should().BeSameAs(mock);
            provider.GetRequiredService<ISmsDeliveryReporter>().Should().BeSameAs(mock);
        }

        [Fact]
        public void AddIranSms_ResolvesClient_AsEachCapabilityInterface()
        {
            var provider = new ServiceCollection()
                .AddIranSms(new MockSmsClient())
                .BuildServiceProvider();

            provider.GetRequiredService<ISmsClient>().Should().BeAssignableTo<ISmsBulkSender>();
            provider.GetRequiredService<ISmsClient>().Should().BeAssignableTo<ISmsOtpSender>();
            provider.GetRequiredService<ISmsClient>().Should().BeAssignableTo<ISmsDeliveryReporter>();
        }

        [Fact]
        public void AddIranSms_RootProviderClient_ExposesCapabilities()
        {
            var kavenegar = new KavenegarClient("api-key");
            var provider = new ServiceCollection()
                .AddIranSms(kavenegar)
                .BuildServiceProvider();

            provider.GetRequiredService<ISmsClient>().Should().BeSameAs(kavenegar);
            provider.GetRequiredService<ISmsBulkSender>().Should().BeSameAs(kavenegar);
            provider.GetRequiredService<ISmsOtpSender>().Should().BeSameAs(kavenegar);
            provider.GetRequiredService<ISmsDeliveryReporter>().Should().BeSameAs(kavenegar);
        }

        [Fact]
        public void AddIranSms_DoesNotRegisterInterfacesTheInstanceDoesNotImplement()
        {
            var minimal = new FakeSmsClient();
            var provider = new ServiceCollection()
                .AddIranSms(minimal)
                .BuildServiceProvider();

            provider.GetRequiredService<ISmsClient>().Should().BeSameAs(minimal);

            // Capability-aware: a non-implemented interface is not registered, so
            // resolving it fails cleanly (InvalidOperationException), never InvalidCastException
            // at the point of a blind forward.
            provider.Invoking(p => p.GetRequiredService<ISmsBulkSender>())
                .Should().Throw<InvalidOperationException>();
            provider.Invoking(p => p.GetRequiredService<ISmsOtpSender>())
                .Should().Throw<InvalidOperationException>();
            provider.Invoking(p => p.GetRequiredService<ISmsDeliveryReporter>())
                .Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AddIranSms_RegistersMinimalClient_WithoutProviderPackage()
        {
            var services = new ServiceCollection();

            var result = services.AddIranSms(new FakeSmsClient());
            var provider = services.BuildServiceProvider();

            result.Should().BeSameAs(services);
            provider.GetRequiredService<ISmsClient>().Should().BeOfType<FakeSmsClient>();
        }

        [Fact]
        public void AddIranSms_RejectsNullServices()
        {
            ServiceCollection? services = null;

            var act = () => services!.AddIranSms(new MockSmsClient());

            act.Should().Throw<ArgumentNullException>().WithParameterName("services");
        }

        [Fact]
        public void AddIranSms_RejectsNullClient()
        {
            var services = new ServiceCollection();
            FakeSmsClient? client = null;

            var act = () => services.AddIranSms(client!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("client");
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
