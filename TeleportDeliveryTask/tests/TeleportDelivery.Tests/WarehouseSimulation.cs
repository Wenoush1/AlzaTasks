using TeleportDelivery.Data;
using TeleportDelivery.Model;
using TeleportDelivery.Planning;

namespace TeleportDelivery.Tests;

/// <summary>
/// Weeks of operation, two trips a day. Before every trip new packages arrive; whatever the planner does
/// not load stays in the warehouse and waits for the next trip. Shows how long packages wait under a policy.
/// </summary>
public static class WarehouseSimulation
{
    // Volume arriving before a trip, as a share of what the fleet can carry: more than fits on most days,
    // much less on Tuesdays and Thursdays. On average 96 % of capacity, so everything can be delivered.
    private const double BusyTripArrivals = 1.15, LightTripArrivals = 0.5;
    private const double AveragePackageVolumeM3 = 0.006;  
    private const int TripsInTwoDays = 4;

    /// <param name="WaitedTwoDays">Share of delivered packages that missed <see cref="TripsInTwoDays"/> or more trips (waited 2 days or longer).</param>
    /// <param name="MaxWait">Most trips missed by any delivered package.</param>
    /// <param name="OldestLeft">Trips missed by the oldest package still waiting at the end.</param>
    /// <param name="OverdueLeftBehind">Times an overdue package did not fit (the waiting limit was broken).</param>
    public sealed record Outcome(double WaitedTwoDays, int MaxWait, int OldestLeft, int OverdueLeftBehind);

    public static Outcome Run(Fleet fleet, WaitingPolicy policy, int weeks, int seed)
    {
        var planner = new LoadPlanner(fleet, policy);
        var warehouse = new List<Package>();
        var deliveredByWait = new List<long>();   // [k] = delivered packages that had missed k trips
        int trips = 0, overdueLeftBehind = 0, nextId = 0;

        for (var day = 0; day < 7 * weeks; day++)
        {
            var lightDay = day % 7 is 1 or 3;   // the week starts on Monday
            for (var tripOfDay = 0; tripOfDay < 2; tripOfDay++, trips++)
            {
                var arrivingVolume = (lightDay ? LightTripArrivals : BusyTripArrivals) * fleet.TotalVolumeM3;
                var arriving = (int)(arrivingVolume / AveragePackageVolumeM3);
                foreach (var p in PackageGenerator.Generate(arriving, ScenarioType.Typical, seed: seed * 10_000 + trips))
                    warehouse.Add(p with { Id = nextId++ });

                Package[] packages = [.. warehouse];
                var plan = planner.Plan(packages);
                overdueLeftBehind += plan.OverdueLeftBehind;

                warehouse.Clear();
                for (var i = 0; i < packages.Length; i++)
                {
                    if (plan.TruckOfPackage[i] == LoadPlan.NotLoaded)
                    {
                        warehouse.Add(packages[i] with { WaitingTrips = packages[i].WaitingTrips + 1 });
                        continue;
                    }
                    var wait = packages[i].WaitingTrips;
                    while (deliveredByWait.Count <= wait) deliveredByWait.Add(0);
                    deliveredByWait[wait]++;
                }
            }
        }

        return new Outcome(
            WaitedTwoDays: (double)deliveredByWait.Skip(TripsInTwoDays).Sum() / deliveredByWait.Sum(),
            MaxWait: deliveredByWait.Count - 1,
            OldestLeft: warehouse.Count == 0 ? 0 : warehouse.Max(p => p.WaitingTrips),
            OverdueLeftBehind: overdueLeftBehind);
    }
}
