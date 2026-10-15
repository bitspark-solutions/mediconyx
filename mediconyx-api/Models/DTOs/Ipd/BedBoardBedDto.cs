using System.ComponentModel.DataAnnotations;
using mediconyx_api.Models;

namespace mediconyx_api.Models.DTOs;

public class BedBoardBedDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Kind { get; set; } = nameof(BedKind.Bed);
    public string? CabinType { get; set; }
    public string? Facilities { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? AdmissionId { get; set; }
    public int? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientNumber { get; set; }
    public string? AttendingDoctorName { get; set; }
    public DateTime? AdmissionDate { get; set; }
}
