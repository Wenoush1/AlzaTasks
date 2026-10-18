namespace TeleportDelivery.Model;

public sealed class LoadPlan
{
    public const int NotLoaded = -1;
    
    public required int[] TruckOfPackage { get; init; }
    public required Truck[] Trucks { get; init; }

    /// <summary>What the planner maximises: profit plus delay cost of the loaded packages (CZK).</summary>
    public required double Value { get; init; }

    /// <summary>No plan that takes all overdue packages can reach a higher <see cref="Value"/>.</summary>
    public required double UpperBound { get; init; }

    /// <summary>Overdue packages that did not fit, i.e. the waiting limit was broken (fleet overloaded).</summary>
    public required int OverdueLeftBehind { get; init; }

    public double TotalProfit => Trucks.Sum(t => t.Profit);
    public int LoadedCount => Trucks.Sum(t => t.PackageCount);

    /// <summary>
    /// How far (relatively) the plan can at most be from the best possible one. Undefined (NaN) when overdue
    /// packages were left behind: then no plan can keep the waiting limit, which the bound assumes.
    /// </summary>
    public double OptimalityGap => OverdueLeftBehind > 0 ? double.NaN
        : UpperBound > 0 ? Math.Max(0, 1 - Value / UpperBound) : 0;
}
