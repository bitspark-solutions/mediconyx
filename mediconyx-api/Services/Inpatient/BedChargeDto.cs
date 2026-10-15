using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Inpatient;

public class BedChargeDto
{
    public int Id { get; set; }
    public DateTime ChargeDate { get; set; }
    public int BedId { get; set; }
    public string BedNumber { get; set; } = string.Empty;
    public string WardName { get; set; } = string.Empty;
    public string WardType { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}
