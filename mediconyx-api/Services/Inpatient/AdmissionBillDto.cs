using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Inpatient;

public class AdmissionBillDto
{
    public int AdmissionId { get; set; }
    public int TenantId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientNumber { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public int TotalDays { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "BDT";
    public List<BedChargeDto> Charges { get; set; } = new();
}
