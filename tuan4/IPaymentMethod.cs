namespace FoodDelivery.Payment;

public enum PaymentTiming
{
    Prepaid,
    OnDelivery
}

public sealed record PaymentResult(bool Success, string TransactionId, string Message)
{
    public static PaymentResult Approved(string transactionId, string message = "Approved") =>
        new(true, transactionId, message);

    public static PaymentResult Declined(string message) =>
        new(false, string.Empty, message);
}

public interface IPaymentMethod
{
    string Name { get; }
    PaymentTiming Timing { get; }
    PaymentResult Charge(long amount);
    PaymentResult Refund(string transactionId, long amount);
}
