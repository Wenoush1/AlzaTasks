namespace TeleportDelivery.Model;

public sealed class LoadPlan
{
    public const int NotLoaded = -1;
    
    public required int[] TruckOfPackage { get; init; }
    public required Truck[] Trucks { get; init; }

    /// <summary>No plan for these packages and this fleet can earn more (CZK).</summary>
    public required double UpperBound { get; init; }

    public double TotalProfit => Trucks.Sum(t => t.Profit);
    public int LoadedCount => Trucks.Sum(t => t.PackageCount);

    /// <summary>How far (relatively) the plan can at most be from the best possible one.</summary>
    public double OptimalityGap => UpperBound > 0 ? Math.Max(0, 1 - TotalProfit / UpperBound) : 0;
}
