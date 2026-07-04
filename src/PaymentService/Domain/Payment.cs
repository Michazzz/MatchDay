namespace PaymentService.Domain;

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2
}


public class Payment
{
    public Guid ReservationId { get; set; }

    public decimal TotalPrice { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
