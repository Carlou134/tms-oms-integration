using Microsoft.Extensions.Time.Testing;
using TmsOmsIntegration.Application.TmsEvents;
using TmsOmsIntegration.Tests.Builders;
using TmsOmsIntegration.Tests.Fakes;

namespace TmsOmsIntegration.Tests.Application.TmsEvents;

public class ReceiveTmsEventTests
{
    private static readonly DateTimeOffset Now = new(2025, 4, 15, 21, 53, 10, TimeSpan.Zero);

    private readonly FakeIdempotencyStore _idempotencyStore = new();
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly ReceiveTmsEvent _receiveTmsEvent;

    public ReceiveTmsEventTests() =>
        _receiveTmsEvent = new ReceiveTmsEvent(_idempotencyStore, _eventPublisher, new FakeTimeProvider(Now));

    [Fact]
    public async Task HandleAsync_NewEvent_IsAcceptedAndPublished()
    {
        var tmsEvent = TmsEventBuilder.Delivered();

        var result = await _receiveTmsEvent.HandleAsync(tmsEvent);

        Assert.Equal(ReceiveTmsEventResult.Accepted, result);
        var published = Assert.IsType<TmsEventReceived>(Assert.Single(_eventPublisher.Published));
        Assert.Equal(tmsEvent, published.Event);
        Assert.Equal(TmsEventKey.From(tmsEvent), published.EventKey);
        Assert.Equal(Now, published.ReceivedAt);
    }

    [Fact]
    public async Task HandleAsync_SameEventTwice_PublishesItOnlyOnce()
    {
        var tmsEvent = TmsEventBuilder.Delivered();

        await _receiveTmsEvent.HandleAsync(tmsEvent);
        var secondResult = await _receiveTmsEvent.HandleAsync(tmsEvent);

        Assert.Equal(ReceiveTmsEventResult.Duplicate, secondResult);
        Assert.Single(_eventPublisher.Published);
    }

    [Fact]
    public async Task HandleAsync_EventsWithDifferentDates_AreBothAccepted()
    {
        var firstVisit = TmsEventBuilder.Delivered();
        var secondVisit = firstVisit with { EventDate = firstVisit.EventDate.AddDays(1) };

        await _receiveTmsEvent.HandleAsync(firstVisit);
        var secondResult = await _receiveTmsEvent.HandleAsync(secondVisit);

        Assert.Equal(ReceiveTmsEventResult.Accepted, secondResult);
        Assert.Equal(2, _eventPublisher.Published.Count);
    }

    [Fact]
    public async Task HandleAsync_WhenPublishFails_RethrowsAndReleasesTheKey()
    {
        var tmsEvent = TmsEventBuilder.Delivered();
        _eventPublisher.FailWith = new InvalidOperationException("Bus unavailable");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _receiveTmsEvent.HandleAsync(tmsEvent));

        Assert.DoesNotContain(TmsEventKey.From(tmsEvent), _idempotencyStore.Keys);
    }

    [Fact]
    public async Task HandleAsync_RetryAfterPublishFailure_IsAcceptedInsteadOfDuplicate()
    {
        var tmsEvent = TmsEventBuilder.Delivered();
        _eventPublisher.FailWith = new InvalidOperationException("Bus unavailable");
        await Assert.ThrowsAsync<InvalidOperationException>(() => _receiveTmsEvent.HandleAsync(tmsEvent));

        _eventPublisher.FailWith = null;
        var retryResult = await _receiveTmsEvent.HandleAsync(tmsEvent);

        Assert.Equal(ReceiveTmsEventResult.Accepted, retryResult);
        Assert.Single(_eventPublisher.Published);
    }
}
