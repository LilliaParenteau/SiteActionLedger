using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteActionLedger.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ServiceRequests",
                type: "TEXT",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteArea",
                table: "ServiceRequests",
                type: "TEXT",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "SiteArea",
                table: "ServiceRequests");
        }
    }
}
