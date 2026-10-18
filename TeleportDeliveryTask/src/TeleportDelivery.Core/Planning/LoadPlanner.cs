using TeleportDelivery.Model;
using TeleportDelivery.Pricing;

namespace TeleportDelivery.Planning;

/// <summary>
/// Plans one trip of the fleet: which packages go and in which truck, so that the profit is maximal.
/// <list type="number">
///   <item>Price the capacity: shadow prices of 1 kg and 1 m³ from the LP relaxation (<see cref="CapacityPricing"/>).</item>
///   <item>Rank packages by efficiency = profit / capacity cost</item>
///   <item>Load greedily in that order; each package goes to the truck in which weight and volume stay
///         the most balanced. Packages that fit nowhere are skipped; smaller ones later fill the gaps.</item>
/// </list>
/// </summary>
public sealed class LoadPlanner(Fleet fleet)
{
    private readonly double[] _perTruckWeight = [.. fleet.Trucks.Select(t => 1 / t.MaxWeightKg)];
    private readonly double[] _perTruckVolume = [.. fleet.Trucks.Select(t => 1 / t.MaxVolumeM3)];

    public LoadPlan Plan(Package[] packages)
    {
        var prices = CapacityPricing.Solve(packages, fleet.TotalWeightKg, fleet.TotalVolumeM3);
        var order = Rank(packages, prices);

        var trucks = new Truck[fleet.Trucks.Count];
        for (var t = 0; t < trucks.Length; t++)
            trucks[t] = new Truck(fleet.Trucks[t]);

        var truckOf = new int[packages.Length];
        Array.Fill(truckOf, LoadPlan.NotLoaded);
        FillTrucks(packages, order, trucks, truckOf);

        return new LoadPlan { TruckOfPackage = truckOf, Trucks = trucks, UpperBound = prices.UpperBound };
    }

    /// <summary>Package indices by efficiency, the best first.</summary>
    private int[] Rank(Package[] packages, CapacityPrices prices)
    {
        // On light days no capacity is scarce, so both prices are 0 and everything fits.
        // Then just value capacity by its share of the fleet, which still gives a sensible order.
        if (prices is { PerKg: 0, PerM3: 0 })
            prices = prices with { PerKg = 1 / fleet.TotalWeightKg, PerM3 = 1 / fleet.TotalVolumeM3 };

        var order = new int[packages.Length];
        var sortKey = new double[packages.Length];
        for (var i = 0; i < packages.Length; i++)
        {
            order[i] = i;
            sortKey[i] = -packages[i].Profit / prices.CapacityCost(packages[i]);   // minus: descending order
        }
        Array.Sort(sortKey, order);
        return order;
    }

    /// <summary>Loads packages in the given order; a package that fits in no truck stays in the warehouse.</summary>
    private void FillTrucks(Package[] packages, int[] order, Truck[] trucks, int[] truckOf)
    {
        // Once trucks are nearly full, most packages fit nowhere. The skyline rejects those in O(log trucks)
        // instead of scanning all trucks. It is rebuilt only when it let through a package that then
        // fitted nowhere: trucks only fill up, so an older skyline still never rejects a package that fits.
        var skyline = new FreeCapacitySkyline(trucks);

        foreach (var i in order)
        {
            ref readonly Package package = ref packages[i];
            if (!skyline.MayFit(package)) continue;

            var truck = MostBalancedTruck(trucks, package);
            if (truck == LoadPlan.NotLoaded)
            {
                skyline = new FreeCapacitySkyline(trucks);
                continue;
            }
            trucks[truck].Add(package);
            truckOf[i] = truck;
        }
    }

    /// <summary>
    /// The truck whose tighter dimension stays the most free after loading (max-min of the free weight
    /// and volume fractions). Heavy packages thus go where kilograms are left and bulky ones where
    /// space is left, so each truck runs out of both at once, instead of one truck being full by weight
    /// with half of its space empty. With one binding limit this is simply "the emptiest truck".
    /// </summary>
    private int MostBalancedTruck(Truck[] trucks, in Package package)
    {
        var bestTruck = LoadPlan.NotLoaded;
        var bestSlack = double.NegativeInfinity;   // free share of the tighter dimension in bestTruck
        for (var t = 0; t < trucks.Length; t++)
        {
            double weightLeft = trucks[t].FreeWeightKg - package.WeightKg;
            double volumeLeft = trucks[t].FreeVolumeM3 - package.VolumeM3;
            if (weightLeft < 0 || volumeLeft < 0) continue;   // does not fit

            double slack = Math.Min(weightLeft * _perTruckWeight[t], volumeLeft * _perTruckVolume[t]);
            if (slack > bestSlack) (bestTruck, bestSlack) = (t, slack);
        }
        return bestTruck;
    }
    
    /// <summary>
    /// Pareto front of free capacity over all trucks: for each free weight, the most free volume in a truck
    /// with at least that much weight left. A package fits in some truck only if a point here covers it.
    /// </summary>
    private sealed class FreeCapacitySkyline
    {
        // Sorted by free weight descending; free volume strictly ascending (dominated trucks are dropped).
        private readonly List<(double FreeWeightKg, double FreeVolumeM3)> _points = [];

        public FreeCapacitySkyline(Truck[] trucks)
        {
            foreach (var truck in trucks.OrderByDescending(t => t.FreeWeightKg))
                if (_points.Count == 0 || truck.FreeVolumeM3 > _points[^1].FreeVolumeM3)
                    _points.Add((truck.FreeWeightKg, truck.FreeVolumeM3));
        }

        public bool MayFit(in Package package)
        {
            var withEnoughWeight = CountWithFreeWeightAtLeast(package.WeightKg);
            return withEnoughWeight > 0 && _points[withEnoughWeight - 1].FreeVolumeM3 >= package.VolumeM3;
        }

        /// <summary>
        /// Points are sorted by free weight descending, so those with at least <paramref name="weightKg"/>
        /// free form a prefix of the list. Binary search for its length.
        /// </summary>
        private int CountWithFreeWeightAtLeast(double weightKg)
        {
            int lo = 0, hi = _points.Count;
            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (_points[mid].FreeWeightKg >= weightKg) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }
    }
}
