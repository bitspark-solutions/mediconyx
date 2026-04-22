namespace mediconyx_api.Models.DTOs;

public class VitalSignDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? AppointmentId { get; set; }
    public int RecordedByUserId { get; set; }
    public string RecordedByName { get; set; } = string.Empty;
    public decimal? TemperatureCelsius { get; set; }
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? HeartRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public int? OxygenSaturation { get; set; }
    public decimal? BloodGlucose { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordedAt { get; set; }
}
