using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

public enum AdmissionType
{
    Emergency = 0,
    Elective = 1,
    Transfer = 2,
    DayCare = 3
}
