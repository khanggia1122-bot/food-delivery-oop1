namespace FoodDelivery.Payment;

public abstract class PaymentMethodBase : IPaymentMethod
{
    private static int transactionCounter;

    private readonly Dictionary<string, long> approved = new();
    private readonly HashSet<string> refunded = new();

    public abstract string Name { get; }
    public abstract PaymentTiming Timing { get; }

    public PaymentResult Charge(long amount)
    {
        if (amount <= 0)
            return PaymentResult.Declined("Amount must be positive");

        PaymentResult result = Authorize(amount);
        if (result.Success)
            approved[result.TransactionId] = amount;
        return result;
    }

    public PaymentResult Refund(string transactionId, long amount)
    {
        if (!approved.TryGetValue(transactionId, out long charged))
            return PaymentResult.Declined("Unknown transaction");
        if (refunded.Contains(transactionId))
            return PaymentResult.Declined("Transaction was already refunded");
        if (amount != charged)
            return PaymentResult.Declined("Refund amount must equal the charged amount");

        Reverse(amount);
        refunded.Add(transactionId);
        return PaymentResult.Approved(transactionId, "Refunded");
    }

    protected abstract PaymentResult Authorize(long amount);
    protected abstract void Reverse(long amount);

    protected static string NewTransactionId(string prefix) =>
        $"{prefix}-{Interlocked.Increment(ref transactionCounter):D4}";
}

public sealed class CashPayment : PaymentMethodBase
{
    private readonly long cashGiven;

    public long ChangeDue { get; private set; }

    public CashPayment(long cashGiven)
    {
        this.cashGiven = cashGiven;
    }

    public override string Name => "Cash on delivery";
    public override PaymentTiming Timing => PaymentTiming.OnDelivery;

    protected override PaymentResult Authorize(long amount)
    {
        if (cashGiven < amount)
            return PaymentResult.Declined("Not enough cash");
        ChangeDue = cashGiven - amount;
        return PaymentResult.Approved(NewTransactionId("CASH"));
    }

    protected override void Reverse(long amount)
    {
    }
}

public sealed class CardPayment : PaymentMethodBase
{
    private readonly string cardNumber;
    private readonly long creditLimit;
    private long creditUsed;

    public long AvailableCredit => creditLimit - creditUsed;

    public CardPayment(string cardNumber, long creditLimit)
    {
        this.cardNumber = new string((cardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        this.creditLimit = creditLimit;
    }

    public override string Name => "Credit card";
    public override PaymentTiming Timing => PaymentTiming.Prepaid;

    protected override PaymentResult Authorize(long amount)
    {
        if (!PassesLuhnCheck(cardNumber))
            return PaymentResult.Declined("Invalid card number");
        if (amount > AvailableCredit)
            return PaymentResult.Declined("Credit limit exceeded");
        creditUsed += amount;
        return PaymentResult.Approved(NewTransactionId("CARD"));
    }

    protected override void Reverse(long amount)
    {
        creditUsed -= amount;
    }

    private static bool PassesLuhnCheck(string digits)
    {
        if (digits.Length < 12) return false;
        int sum = 0;
        bool doubleIt = false;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int d = digits[i] - '0';
            if (doubleIt)
            {
                d *= 2;
                if (d > 9) d -= 9;
            }
            sum += d;
            doubleIt = !doubleIt;
        }
        return sum % 10 == 0;
    }
}

public sealed class EWalletPayment : PaymentMethodBase
{
    public string Owner { get; }
    public long Balance { get; private set; }

    public EWalletPayment(string owner, long balance)
    {
        Owner = owner;
        Balance = balance;
    }

    public override string Name => $"E-wallet ({Owner})";
    public override PaymentTiming Timing => PaymentTiming.Prepaid;

    protected override PaymentResult Authorize(long amount)
    {
        if (Balance < amount)
            return PaymentResult.Declined("Insufficient wallet balance");
        Balance -= amount;
        return PaymentResult.Approved(NewTransactionId("WALLET"));
    }

    protected override void Reverse(long amount)
    {
        Balance += amount;
    }
}
