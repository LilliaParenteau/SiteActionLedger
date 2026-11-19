using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace  SiteActionLedger.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicians : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TechnicianId",
                table: "WorkOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Technician",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Specialty = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technician", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TechnicianId",
                table: "WorkOrders",
                column: "TechnicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_Technician_TechnicianId",
                table: "WorkOrders",
                column: "TechnicianId",
                principalTable: "Technician",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_Technician_TechnicianId",
                table: "WorkOrders");

            migrationBuilder.DropTable(
                name: "Technician");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_TechnicianId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "TechnicianId",
                table: "WorkOrders");
        }
    }
}
