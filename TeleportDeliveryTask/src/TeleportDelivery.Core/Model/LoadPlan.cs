namespace TeleportDelivery.Model;

public sealed class LoadPlan
{
    public const int NotLoaded = -1;
    
    public required int[] TruckOfPackage { get; init; }
    public required Truck[] Trucks { get; init; }

    public double TotalProfit => Trucks.Sum(t => t.Profit);
    public int LoadedCount => Trucks.Sum(t => t.PackageCount);
}
