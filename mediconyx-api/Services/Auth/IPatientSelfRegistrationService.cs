using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Services.Auth;

public interface IPatientSelfRegistrationService
{
    Task<RegisterPatientResponse> RegisterAsync(RegisterPatientRequest request, string? remoteIp, CancellationToken ct = default);
    Task<VerifyEmailResponse> VerifyEmailAsync(string token, CancellationToken ct = default);
    Task<bool> ResendVerificationAsync(string email, CancellationToken ct = default);
}
