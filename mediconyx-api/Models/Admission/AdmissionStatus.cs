using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

public enum AdmissionStatus
{
    Active = 0,
    Discharged = 1,
    Transferred = 2,    // patient moved to another facility
    LAMA = 3,           // Leave Against Medical Advice
    Deceased = 4
}
