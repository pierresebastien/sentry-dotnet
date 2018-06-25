using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Sentry.Extensions.Logging;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    [Collection(nameof(SentrySdkCollection))]
    public class AspNetSentryWebHostBuilderExtensionsTests : AspNetSentrySdkTestFixture
    {
        public IWebHostBuilder WebHostBuilder { get; set; } = Substitute.For<IWebHostBuilder>();
        public ServiceCollection Services { get; set; } = new ServiceCollection();
        public IConfiguration Configuration { get; set; } = Substitute.For<IConfiguration>();

        public AspNetSentryWebHostBuilderExtensionsTests()
        {
            var context = new WebHostBuilderContext { Configuration = Configuration };

            WebHostBuilder
                .When(b => b.ConfigureServices(Arg.Any<Action<IServiceCollection>>()))
                .Do(i => i.Arg<Action<IServiceCollection>>()(Services));

            WebHostBuilder
                .When(b => b.ConfigureServices(Arg.Any<Action<WebHostBuilderContext, IServiceCollection>>()))
                .Do(i => i.Arg<Action<WebHostBuilderContext, IServiceCollection>>()(context, Services));
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_ValidDsnString_ServicesRegistered(Action<IServiceCollection> assert)
        {
            WebHostBuilder.UseSentry(DsnSamples.ValidDsnWithoutSecret);
            try
            {
                assert(Services);
                Assert.True(SentrySdk.IsEnabled);
            }
            finally
            {
                SentrySdk.Close();
            }
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_Parameterless_ServicesRegistered(Action<IServiceCollection> assert)
        {
            WebHostBuilder.UseSentry();
            assert(Services);
            Assert.False(SentrySdk.IsEnabled);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_DisableDsnString_ServicesRegistered(Action<IServiceCollection> assert)
        {
            WebHostBuilder.UseSentry(Internal.Constants.DisableSdkDsnValue);
            assert(Services);
            Assert.False(SentrySdk.IsEnabled);
        }

        [Theory, MemberData(nameof(ExpectedServices))]
        public void UseSentry_Callback_ServicesRegistered(Action<IServiceCollection> assert)
        {
            WebHostBuilder.UseSentry(o => o.InitializeSdk = false);
            assert(Services);
            Assert.False(SentrySdk.IsEnabled);
        }

        public static IEnumerable<object[]> ExpectedServices()
        {
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ServiceType == typeof(IHub)))};
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ImplementationInstance?.GetType() == typeof(SentryLoggerProvider)))};
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ServiceType == typeof(SentryAspNetCoreOptions)))};
            yield return new object[] {
                new Action<IServiceCollection>(c =>
                    Assert.Single(c, d => d.ImplementationType == typeof(SentryStartupFilter)))};
        }
    }
}