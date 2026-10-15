using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Auth;
using mediconyx_api.Services.Common;
using System.Security.Claims;

namespace mediconyx_api.Controllers;

public class ResendVerificationRequest
{
    public string Email { get; set; } = string.Empty;
}
