using TeleportDelivery.Model;

namespace TeleportDelivery.Planning;

/// <summary>
/// Plans one trip of the fleet: which packages go and in which truck. A first, naive greedy version:
/// the most profitable packages are loaded first, each into the first truck with enough room. Packages
/// that fit nowhere are skipped; smaller ones later fill the gaps.
/// </summary>
public sealed class LoadPlanner(Fleet fleet)
{
    public LoadPlan Plan(Package[] packages)
    {
        var order = Rank(packages);

        var trucks = new Truck[fleet.Trucks.Count];
        for (var t = 0; t < trucks.Length; t++)
            trucks[t] = new Truck(fleet.Trucks[t]);

        var truckOf = new int[packages.Length];
        Array.Fill(truckOf, LoadPlan.NotLoaded);
        FillTrucks(packages, order, trucks, truckOf);

        return new LoadPlan { TruckOfPackage = truckOf, Trucks = trucks };
    }

    /// <summary>Package indices, the most profitable first.</summary>
    private static int[] Rank(Package[] packages)
    {
        var order = new int[packages.Length];
        var sortKey = new double[packages.Length];
        for (var i = 0; i < packages.Length; i++)
        {
            order[i] = i;
            sortKey[i] = -packages[i].Profit;
        }
        Array.Sort(sortKey, order);
        return order;
    }
    
    private static void FillTrucks(Package[] packages, int[] order, Truck[] trucks, int[] truckOf)
    {
        foreach (var i in order)
        {
            ref readonly Package package = ref packages[i];
            var truck = FirstFittingTruck(trucks, package);
            if (truck == LoadPlan.NotLoaded) continue;

            trucks[truck].Add(package);
            truckOf[i] = truck;
        }
    }

    private static int FirstFittingTruck(Truck[] trucks, in Package package)
    {
        for (var t = 0; t < trucks.Length; t++)
            if (trucks[t].Fits(package)) return t;
        return LoadPlan.NotLoaded;
    }
}
