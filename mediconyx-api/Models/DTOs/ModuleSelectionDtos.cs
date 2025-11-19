namespace mediconyx_api.Models.DTOs;

public class ModuleSelectionDto
{
    public bool OpdManagement { get; set; }
    public bool IpdManagement { get; set; }
    public bool EmergencyServices { get; set; }
    public bool PharmacyManagement { get; set; }
    public bool Laboratory { get; set; }
    public bool RadiologyImaging { get; set; }
    public bool BillingInvoicing { get; set; }
    public bool InventoryManagement { get; set; }
    public bool HrPayroll { get; set; }
    public bool AppointmentScheduling { get; set; }
    public bool Telemedicine { get; set; }
    public bool AnalyticsReporting { get; set; }
}

public class ModuleSelectionResponseDto : ModuleSelectionDto
{
    public int Id { get; set; }
    public int SalesRequestId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
