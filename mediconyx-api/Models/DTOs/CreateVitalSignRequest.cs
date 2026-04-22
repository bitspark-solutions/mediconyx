namespace mediconyx_api.Models.DTOs;

public class CreateVitalSignRequest
{
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
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
}
