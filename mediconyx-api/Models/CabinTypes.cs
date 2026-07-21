namespace mediconyx_api.Models;

/// <summary>
/// Cabin categories with their default amenities and typical per-day rate guidance (BDT),
/// based on rate cards of major Bangladeshi private hospitals (Square, Popular, BRB, AMZ).
/// A hospital can override facilities and rates per cabin; these are sensible defaults.
/// </summary>
public static class CabinTypes
{
    public const string Shared = "Shared";
    public const string Standard = "Standard";
    public const string Deluxe = "Deluxe";
    public const string VIP = "VIP";
    public const string VVIP = "VVIP";
    public const string Suite = "Suite";

    public static readonly string[] All = { Shared, Standard, Deluxe, VIP, VVIP, Suite };

    /// <summary>Default amenities per cabin type. Stored on the Bed as a JSON array.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> DefaultFacilities =
        new Dictionary<string, string[]>
    {
        [Shared]  = new[] { "Shared Bathroom", "Attendant Seat", "Fan", "Nurse Call" },
        [Standard] = new[] { "AC", "Attached Bathroom", "Attendant Bed", "Nurse Call" },
        [Deluxe]  = new[] { "AC", "Attached Bathroom", "TV", "Attendant Bed", "WiFi", "Nurse Call", "Hot Water" },
        [VIP]     = new[] { "AC", "Attached Bathroom", "TV", "Attendant Bed", "Sofa", "Refrigerator", "WiFi", "Nurse Call", "Hot Water", "Oxygen Outlet" },
        [VVIP]    = new[] { "AC", "Attached Bathroom", "TV", "Attendant Bed", "Sofa Set", "Refrigerator", "Microwave", "WiFi", "Nurse Call", "Hot Water", "Oxygen Outlet", "Meals Included" },
        [Suite]   = new[] { "Premium AC", "Luxury Bathroom", "Smart TV", "Attendant Room", "Living Area", "Refrigerator", "Microwave", "WiFi", "Dedicated Nurse Call", "Hot Water", "Oxygen Outlet", "Meals Included", "Visitor Lounge Access" },
    };

    /// <summary>Typical per-day rate range in BDT (informational; hospitals set their own DailyRate).</summary>
    public static readonly IReadOnlyDictionary<string, (decimal Min, decimal Max)> TypicalRatesBdt =
        new Dictionary<string, (decimal, decimal)>
    {
        [Shared]  = (3000m, 6500m),
        [Standard] = (6000m, 12000m),
        [Deluxe]  = (10000m, 15000m),
        [VIP]     = (13000m, 20000m),
        [VVIP]    = (18000m, 30000m),
        [Suite]   = (30000m, 50000m),
    };
}
