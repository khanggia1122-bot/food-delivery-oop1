namespace FoodDelivery.Pricing;

public interface IFeePolicy
{
    string Name { get; }
    long CalculateFee(double distanceKm);
}

public sealed class DistanceFeePolicy : IFeePolicy
{
    private readonly long baseFee;
    private readonly long perKm;

    public DistanceFeePolicy(long baseFee, long perKm)
    {
        if (baseFee < 0 || perKm < 0)
            throw new ArgumentOutOfRangeException(nameof(baseFee), "Fees must not be negative");
        this.baseFee = baseFee;
        this.perKm = perKm;
    }

    public string Name => "DistanceFee";

    public long CalculateFee(double distanceKm) =>
        baseFee + (long)Math.Round(perKm * distanceKm, MidpointRounding.AwayFromZero);
}

public sealed class SurgeFeePolicy : IFeePolicy
{
    private readonly IFeePolicy inner;
    private readonly int percent;

    public SurgeFeePolicy(IFeePolicy inner, int percent)
    {
        if (percent <= 0)
            throw new ArgumentOutOfRangeException(nameof(percent), "Surge percent must be positive");
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.percent = percent;
    }

    public string Name => $"SurgeFee({inner.Name} x{percent}%)";

    public long CalculateFee(double distanceKm) => inner.CalculateFee(distanceKm) * percent / 100;
}
