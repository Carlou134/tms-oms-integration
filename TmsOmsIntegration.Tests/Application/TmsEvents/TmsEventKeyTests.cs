using TmsOmsIntegration.Application.TmsEvents;
using TmsOmsIntegration.Domain.Orders;
using TmsOmsIntegration.Tests.Builders;

namespace TmsOmsIntegration.Tests.Application.TmsEvents;

public class TmsEventKeyTests
{
    private readonly TmsEvent _event = TmsEventBuilder.Delivered();

    [Fact]
    public void From_SameEvent_ReturnsSameKey()
    {
        Assert.Equal(TmsEventKey.From(_event), TmsEventKey.From(_event with { }));
    }

    [Fact]
    public void From_DifferentCaseOrSurroundingSpaces_ReturnsSameKey()
    {
        var resent = _event with { OrderNumber = " 2500000006-01 ", SubStatus = "client received" };

        Assert.Equal(TmsEventKey.From(_event), TmsEventKey.From(resent));
    }

    [Fact]
    public void From_SameInstantInAnotherOffset_ReturnsSameKey()
    {
        var sameInstantInUtc = _event with { EventDate = _event.EventDate.ToUniversalTime() };

        Assert.Equal(TmsEventKey.From(_event), TmsEventKey.From(sameInstantInUtc));
    }

    [Fact]
    public void From_FieldsOutsideTheKey_DoNotChangeIt()
    {
        var withOtherDetails = _event with { CourierName = "Conductor 12", Comments = "Otro comentario" };

        Assert.Equal(TmsEventKey.From(_event), TmsEventKey.From(withOtherDetails));
    }

    public static TheoryData<TmsEvent> EventsThatDifferInAKeyField
    {
        get
        {
            var baseEvent = TmsEventBuilder.Delivered();
            return
            [
                baseEvent with { OrderNumber = "2500000007-01" },
                baseEvent with { ServiceType = ServiceType.Return },
                baseEvent with { Status = OrderStatus.NotDelivered },
                baseEvent with { SubStatus = "CLIENT ABSENT" },
                baseEvent with { EventDate = baseEvent.EventDate.AddSeconds(1) }
            ];
        }
    }

    [Theory]
    [MemberData(nameof(EventsThatDifferInAKeyField))]
    public void From_DifferentKeyField_ReturnsDifferentKey(TmsEvent otherEvent)
    {
        Assert.NotEqual(TmsEventKey.From(_event), TmsEventKey.From(otherEvent));
    }
}
