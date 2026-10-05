using TmsOmsIntegration.Application.Notifications;
using TmsOmsIntegration.Tests.Fakes;

namespace TmsOmsIntegration.Tests.Application.Notifications;

public class NotificationFormatterResolverTests
{
    private readonly StubNotificationFormatter _defaultFormatter = new(clientCode: null);
    private readonly StubNotificationFormatter _tiendasPeruanasFormatter = new(clientCode: "01021755");
    private readonly NotificationFormatterResolver _resolver;

    public NotificationFormatterResolverTests() =>
        _resolver = new NotificationFormatterResolver([_defaultFormatter, _tiendasPeruanasFormatter]);

    [Fact]
    public void Resolve_ClientWithOwnFormatter_ReturnsItsFormatter()
    {
        Assert.Same(_tiendasPeruanasFormatter, _resolver.Resolve("01021755"));
    }

    [Fact]
    public void Resolve_ClientCodeWithSurroundingSpaces_ReturnsItsFormatter()
    {
        Assert.Same(_tiendasPeruanasFormatter, _resolver.Resolve(" 01021755 "));
    }

    [Theory]
    [InlineData("01021800")]
    [InlineData("01021900")]
    [InlineData(null)]
    public void Resolve_ClientWithoutOwnFormatter_ReturnsDefaultFormatter(string? clientCode)
    {
        Assert.Same(_defaultFormatter, _resolver.Resolve(clientCode));
    }

    [Fact]
    public void Constructor_WithoutDefaultFormatter_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new NotificationFormatterResolver([new StubNotificationFormatter("01021755")]));

        Assert.Contains("default notification formatter", exception.Message);
    }

    [Fact]
    public void Constructor_WithTwoDefaultFormatters_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new NotificationFormatterResolver([new StubNotificationFormatter(null), new StubNotificationFormatter(null)]));
    }

    [Fact]
    public void Constructor_WithTwoFormattersForTheSameClient_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new NotificationFormatterResolver(
            [
                new StubNotificationFormatter(null),
                new StubNotificationFormatter("01021755"),
                new StubNotificationFormatter("01021755")
            ]));
    }
}
