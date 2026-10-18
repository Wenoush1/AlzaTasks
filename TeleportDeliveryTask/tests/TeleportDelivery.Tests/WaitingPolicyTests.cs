using TeleportDelivery.Model;

namespace TeleportDelivery.Tests;

/// <summary>Four simulated weeks at 1/10 scale (12 trucks), the same arrivals for every policy.</summary>
public class WaitingPolicyTests
{
    private const int Weeks = 4, Seed = 7;

    private static readonly Fleet SmallerFleet = new(Fleet.Default.Trucks.Take(Fleet.Default.Trucks.Count / 10));

    private static WarehouseSimulation.Outcome Simulate(WaitingPolicy policy) =>
        WarehouseSimulation.Run(SmallerFleet, policy, Weeks, Seed);

    [Fact]
    public void DefaultPolicy_NobodyWaitsLongerThanTheLimit()
    {
        var limit = WaitingPolicy.Default.MaxWaitingTrips!.Value;

        var outcome = Simulate(WaitingPolicy.Default);

        Assert.Equal(0, outcome.OverdueLeftBehind);
        Assert.True(outcome.MaxWait <= limit, $"a package missed {outcome.MaxWait} trips");
        Assert.True(outcome.OldestLeft <= limit, $"a package still waiting has missed {outcome.OldestLeft} trips");
    }

    [Fact]
    public void DefaultPolicy_CutsLongWaitsComparedToProfitOnly()
    {
        var profitOnly = Simulate(WaitingPolicy.ProfitOnly);
        var recommended = Simulate(WaitingPolicy.Default);

        Assert.True(recommended.WaitedTwoDays < profitOnly.WaitedTwoDays,
            $"waited 2+ days: {recommended.WaitedTwoDays:P2} with the default policy, {profitOnly.WaitedTwoDays:P2} with profit only");
        Assert.True(recommended.MaxWait < profitOnly.MaxWait,
            $"longest wait: {recommended.MaxWait} trips with the default policy, {profitOnly.MaxWait} with profit only");
    }
}
