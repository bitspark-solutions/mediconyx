namespace mediconyx_api.Models.DTOs;

public class PatientHealthSummary
{
    public int PatientId { get; set; }
    public string SummaryText { get; set; } = string.Empty;
    public List<string> ActiveConditions { get; set; } = new();
    public List<string> CurrentMedications { get; set; } = new();
    public List<string> RecentLabResults { get; set; } = new();
    public List<string> UpcomingAppointments { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public float Confidence { get; set; }
}
