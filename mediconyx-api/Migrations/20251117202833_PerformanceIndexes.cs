using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mediconyx_api.Migrations
{
    /// <inheritdoc />
    public partial class PerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesRequests_OwnerSalesUserId",
                table: "SalesRequests");

            migrationBuilder.DropIndex(
                name: "IX_SalesRequestNotes_SalesRequestId",
                table: "SalesRequestNotes");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingTimelineEvents_SalesRequestId",
                table: "OnboardingTimelineEvents");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_Email",
                table: "SalesRequests",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_OwnerSalesUserId_Status",
                table: "SalesRequests",
                columns: new[] { "OwnerSalesUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_PaidAt",
                table: "SalesRequests",
                column: "PaidAt",
                filter: "\"PaidAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_Region_Status",
                table: "SalesRequests",
                columns: new[] { "Region", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_Status_CreatedAt",
                table: "SalesRequests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequestNotes_SalesRequestId_CreatedAt",
                table: "SalesRequestNotes",
                columns: new[] { "SalesRequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequestAttachments_Sha256",
                table: "SalesRequestAttachments",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTimelineEvents_SalesRequestId_HappenedAt",
                table: "OnboardingTimelineEvents",
                columns: new[] { "SalesRequestId", "HappenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesRequests_Email",
                table: "SalesRequests");

            migrationBuilder.DropIndex(
                name: "IX_SalesRequests_OwnerSalesUserId_Status",
                table: "SalesRequests");

            migrationBuilder.DropIndex(
                name: "IX_SalesRequests_PaidAt",
                table: "SalesRequests");

            migrationBuilder.DropIndex(
                name: "IX_SalesRequests_Region_Status",
                table: "SalesRequests");

            migrationBuilder.DropIndex(
                name: "IX_SalesRequests_Status_CreatedAt",
                table: "SalesRequests");

            migrationBuilder.DropIndex(
                name: "IX_SalesRequestNotes_SalesRequestId_CreatedAt",
                table: "SalesRequestNotes");

            migrationBuilder.DropIndex(
                name: "IX_SalesRequestAttachments_Sha256",
                table: "SalesRequestAttachments");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingTimelineEvents_SalesRequestId_HappenedAt",
                table: "OnboardingTimelineEvents");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityName_EntityId_CreatedAt",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_OwnerSalesUserId",
                table: "SalesRequests",
                column: "OwnerSalesUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequestNotes_SalesRequestId",
                table: "SalesRequestNotes",
                column: "SalesRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTimelineEvents_SalesRequestId",
                table: "OnboardingTimelineEvents",
                column: "SalesRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });
        }
    }
}
