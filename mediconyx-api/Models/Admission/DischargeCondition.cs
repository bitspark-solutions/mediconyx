using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

public enum DischargeCondition
{
    Improved = 0,
    Stable = 1,
    Critical = 2,
    LAMA = 3,
    Deceased = 4
}
