using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteActionTrace.Migrations
{
    /// <inheritdoc />
    public partial class LinkServiceRequestsToProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "ServiceRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_ProjectId",
                table: "ServiceRequests",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequests_Projects_ProjectId",
                table: "ServiceRequests",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequests_Projects_ProjectId",
                table: "ServiceRequests");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_ProjectId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "ServiceRequests");
        }
    }
}
