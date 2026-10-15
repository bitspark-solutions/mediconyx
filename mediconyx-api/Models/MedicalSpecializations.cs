using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

public static class MedicalSpecializations
{
    public const string General = "General";
    public const string Cardiology = "Cardiology";
    public const string Orthopedics = "Orthopedics";
    public const string Neurology = "Neurology";
    public const string Pediatrics = "Pediatrics";
    public const string Dermatology = "Dermatology";
    public const string InternalMedicine = "Internal Medicine";
    public const string Surgery = "Surgery";
    public const string Radiology = "Radiology";
    public const string Pathology = "Pathology";
    public const string Psychiatry = "Psychiatry";
    public const string Ophthalmology = "Ophthalmology";
    public const string ENT = "ENT";
    public const string Nephrology = "Nephrology";
    public const string Gastroenterology = "Gastroenterology";
    public const string Pulmonology = "Pulmonology";
    public const string Endocrinology = "Endocrinology";
    public const string Oncology = "Oncology";
    public const string Other = "Other";

    public static readonly string[] All = {
        General, Cardiology, Orthopedics, Neurology, Pediatrics, Dermatology,
        InternalMedicine, Surgery, Radiology, Pathology, Psychiatry,
        Ophthalmology, ENT, Nephrology, Gastroenterology, Pulmonology,
        Endocrinology, Oncology, Other
    };
}
