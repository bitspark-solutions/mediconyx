using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;

namespace mediconyx_api.Services.Inpatient;

public class BillingResult
{
    public int AdmissionId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int NewChargesCreated { get; set; }
    public int TotalChargeDays { get; set; }
    public decimal TotalAmount { get; set; }
}
