using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

public static class WardTypes
{
    public const string General = "General";
    public const string ICU = "ICU";
    public const string PICU = "PICU";
    public const string NICU = "NICU";
    public const string DayCare = "DayCare";
    public const string Maternity = "Maternity";
    public const string Pediatric = "Pediatric";
    public const string Geriatric = "Geriatric";
    public const string Surgery = "Surgery";
    public const string Cabin = "Cabin";

    public static readonly string[] All = {
        General, ICU, PICU, NICU, DayCare, Maternity, Pediatric, Geriatric, Surgery, Cabin
    };
}
