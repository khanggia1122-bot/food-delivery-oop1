using FoodDelivery.Domain;

namespace FoodDelivery.Dispatch;

public interface IDriverAssigner
{
    DeliveryDriver Assign(Order order, IReadOnlyList<DeliveryDriver> drivers);
}

public sealed class NearestDriverAssigner : IDriverAssigner
{
    public DeliveryDriver Assign(Order order, IReadOnlyList<DeliveryDriver> drivers)
    {
        Location from = order.Restaurant.Location;
        DeliveryDriver? best = null;
        double bestDistance = 0;

        foreach (DeliveryDriver driver in drivers)
        {
            if (!driver.IsAvailable) continue;
            double distance = from.DistanceTo(driver.Position);
            if (best is null || distance < bestDistance)
            {
                best = driver;
                bestDistance = distance;
            }
        }

        if (best is null)
            throw new InvalidOperationException("No available driver");

        order.Dispatch(best);
        return best;
    }
}
