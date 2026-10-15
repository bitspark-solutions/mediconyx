using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

public static class ServiceTypes
{
    public const string Consultation = "Consultation";
    public const string Therapy = "Therapy";
    public const string Surgery = "Surgery";
    public const string Diagnosis = "Diagnosis";
    public const string Lab = "Lab";
    public const string Radiology = "Radiology";
    public const string Pharmacy = "Pharmacy";
    public const string Emergency = "Emergency";
    public const string ICU = "ICU";
    public const string PICU = "PICU";
    public const string NICU = "NICU";
    public const string DayCare = "DayCare";
    public const string Maternity = "Maternity";
    public const string Pediatric = "Pediatric";
    public const string Geriatric = "Geriatric";
    public const string General = "General";
    public const string Other = "Other";

    public static readonly string[] All = {
        Consultation, Therapy, Surgery, Diagnosis, Lab, Radiology, Pharmacy, Emergency,
        ICU, PICU, NICU, DayCare, Maternity, Pediatric, Geriatric, General, Other
    };

    /// <summary>Service types that are wards (have beds). The rest are OPD services.</summary>
    public static readonly string[] WardTypes = {
        General, ICU, PICU, NICU, DayCare, Maternity, Pediatric, Geriatric, Surgery
    };
}
