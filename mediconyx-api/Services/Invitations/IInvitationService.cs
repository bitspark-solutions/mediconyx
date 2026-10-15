using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using mediconyx_api.Data;
using mediconyx_api.Models;
using mediconyx_api.Services.Infrastructure;

namespace mediconyx_api.Services.Invitations;

public interface IInvitationService
{
    /// <summary>Create a pre-tenant pipeline invite tied to a SalesRequest (IntendedRole=Admin) and email the link.</summary>
    Task<Invitation> CreatePipelineInvitationAsync(SalesRequest request, int? invitedByUserId, CancellationToken ct = default);

    /// <summary>Create a tenant self-service invite (tenant already exists) and email the link.</summary>
    Task<Invitation> CreateTenantInvitationAsync(int tenantId, string email, UserRole role, int? invitedByUserId, CancellationToken ct = default);

    /// <summary>Validate a token. Returns the invitation if valid (Pending + not expired), else null.</summary>
    Task<Invitation?> ValidateTokenAsync(string token, CancellationToken ct = default);

    /// <summary>Accept an invite: store the chosen password hash and mark Accepted. For tenant invites, also create the user.</summary>
    Task<AcceptInvitationResult> AcceptInvitationAsync(string token, string password, string confirmPassword, CancellationToken ct = default);

    /// <summary>Regenerate the token + resend the invite email.</summary>
    Task<bool> ResendAsync(int invitationId, CancellationToken ct = default);

    /// <summary>All invitations for a given pipeline request (most recent first).</summary>
    Task<List<Invitation>> GetBySalesRequestAsync(int salesRequestId, CancellationToken ct = default);

    /// <summary>Find the latest Accepted (not yet consumed) pipeline invite for an email — used at provisioning.</summary>
    Task<Invitation?> GetAcceptedPipelineInvitationForEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Mark an invitation consumed once its password hash has been used to create a user.</summary>
    Task MarkConsumedAsync(int invitationId, int createdUserId, CancellationToken ct = default);
}
