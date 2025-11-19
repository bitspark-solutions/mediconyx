namespace mediconyx_api.Models;

public class ModuleSelection
{
    public int Id { get; set; }
    
    // Link to SalesRequest (1:1 relationship)
    public int SalesRequestId { get; set; }
    public SalesRequest SalesRequest { get; set; } = null!;
    
    // Module Flags
    public bool OpdManagement { get; set; } = false;
    public bool IpdManagement { get; set; } = false;
    public bool EmergencyServices { get; set; } = false;
    public bool PharmacyManagement { get; set; } = false;
    public bool Laboratory { get; set; } = false;
    public bool RadiologyImaging { get; set; } = false;
    public bool BillingInvoicing { get; set; } = false;
    public bool InventoryManagement { get; set; } = false;
    public bool HrPayroll { get; set; } = false;
    public bool AppointmentScheduling { get; set; } = false;
    public bool Telemedicine { get; set; } = false;
    public bool AnalyticsReporting { get; set; } = false;
    
    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
