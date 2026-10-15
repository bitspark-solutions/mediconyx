namespace mediconyx_api.Models.DTOs;

public class AdminClinicalKpiDto
{
    public int ActiveAdmissions { get; set; }
    public int TotalBeds { get; set; }
    public decimal BedOccupancyPercent { get; set; }
    public int DischargedPatients { get; set; }
    public decimal AverageLengthOfStay { get; set; }
    public int DeceasedPatients { get; set; }
    public decimal MortalityRatePercent { get; set; }
    public int NewBirths { get; set; }
}
