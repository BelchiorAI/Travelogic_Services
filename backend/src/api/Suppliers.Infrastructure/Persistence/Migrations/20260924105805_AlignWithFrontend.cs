using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suppliers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignWithFrontend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address",
                schema: "supplier",
                table: "Suppliers",
                newName: "AddressLine");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "supplier",
                table: "Services",
                type: "bit",
                nullable: false,
                // Services that already exist were bookable, so they start out active.
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "supplier",
                table: "Services");

            migrationBuilder.RenameColumn(
                name: "AddressLine",
                schema: "supplier",
                table: "Suppliers",
                newName: "Address");
        }
    }
}
