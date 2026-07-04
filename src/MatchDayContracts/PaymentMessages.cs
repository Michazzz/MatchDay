namespace MatchDayContracts;

/// <summary>Command: ask PaymentService to process payment.</summary>
public record ProcessPayment
{
    public Guid ReservationId { get; init; }
    public decimal TotalPrice { get; init; }
}

/// <summary>Event: PaymentService successfully held the payment.</summary>
public record PaymentCompleted
{
    public Guid ReservationId { get; init; }

    public decimal TotalPrice { get; init; }
}

/// <summary>Event: PaymentService could not process payment.</summary>
public record PaymentFailed
{
    public Guid ReservationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
