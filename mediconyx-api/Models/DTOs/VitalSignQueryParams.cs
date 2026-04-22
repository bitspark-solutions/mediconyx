namespace mediconyx_api.Models.DTOs;

public class VitalSignQueryParams
{
    public int? PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
