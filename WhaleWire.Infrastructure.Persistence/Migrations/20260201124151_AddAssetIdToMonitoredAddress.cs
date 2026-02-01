using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhaleWire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetIdToMonitoredAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_monitored_addresses",
                table: "monitored_addresses");

            migrationBuilder.AddColumn<string>(
                name: "asset_id",
                table: "monitored_addresses",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_monitored_addresses",
                table: "monitored_addresses",
                columns: new[] { "chain", "address", "provider", "asset_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_monitored_addresses",
                table: "monitored_addresses");

            migrationBuilder.DropColumn(
                name: "asset_id",
                table: "monitored_addresses");

            migrationBuilder.AddPrimaryKey(
                name: "PK_monitored_addresses",
                table: "monitored_addresses",
                columns: new[] { "chain", "address", "provider" });
        }
    }
}
