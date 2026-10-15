using System.ComponentModel.DataAnnotations;

namespace mediconyx_api.Models;

public enum TelehealthSessionStatus
{
    Scheduled = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3,
    NoShow = 4
}
