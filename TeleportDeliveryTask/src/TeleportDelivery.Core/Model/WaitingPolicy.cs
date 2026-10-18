namespace TeleportDelivery.Model;

/// <summary>
/// How waiting packages gain priority. Every package has to be delivered one day, so leaving one in the
/// warehouse is never free: the customer waits, gets annoyed and may cancel. That cost grows with every
/// missed trip, so it is added to what loading the package is worth. A hard limit then guarantees that no
/// package waits longer than the business promises, whatever its profit.
/// </summary>
/// <param name="DelayCostPerTrip">CZK added to a package's value for every trip it has already missed.</param>
/// <param name="MaxWaitingTrips">A package that has missed this many trips must go on the next one (null = no limit).</param>
public sealed record WaitingPolicy(double DelayCostPerTrip, int? MaxWaitingTrips)
{
    /// <summary>
    /// 50 CZK per missed trip, and nobody waits more than 2 days (4 trips). The delay cost keeps the number
    /// of long waits low, the limit caps the worst case. Both are business decisions.
    /// </summary>
    public static WaitingPolicy Default { get; } = new(DelayCostPerTrip: 50, MaxWaitingTrips: 4);

    /// <summary>Each trip in isolation, as literally stated in the assignment (for comparison).</summary>
    public static WaitingPolicy ProfitOnly { get; } = new(DelayCostPerTrip: 0, MaxWaitingTrips: null);

    /// <summary>What loading the package on this trip is worth: its profit plus the delay cost it stops.</summary>
    public double Value(in Package p) => p.Profit + DelayCostPerTrip * p.WaitingTrips;

    /// <summary>The package has waited as long as allowed and must go now.</summary>
    public bool IsOverdue(in Package p) => p.WaitingTrips >= MaxWaitingTrips;
}
