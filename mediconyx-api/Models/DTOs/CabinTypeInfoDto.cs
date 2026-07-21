namespace mediconyx_api.Models.DTOs;

/// <summary>Static catalog entry describing a cabin category (GET /api/ipd/cabin-types).</summary>
public class CabinTypeInfoDto
{
    public string Type { get; set; } = string.Empty;
    public string[] DefaultFacilities { get; set; } = Array.Empty<string>();
    public decimal TypicalRateMinBdt { get; set; }
    public decimal TypicalRateMaxBdt { get; set; }
}
