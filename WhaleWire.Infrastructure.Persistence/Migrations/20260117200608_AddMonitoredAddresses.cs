using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhaleWire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoredAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "monitored_addresses",
                columns: table => new
                {
                    chain = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    address = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    balance = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    discovered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monitored_addresses", x => new { x.chain, x.address, x.provider });
                });

            migrationBuilder.CreateIndex(
                name: "IX_monitored_addresses_balance",
                table: "monitored_addresses",
                column: "balance");

            migrationBuilder.CreateIndex(
                name: "IX_monitored_addresses_is_active",
                table: "monitored_addresses",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "monitored_addresses");
        }
    }
}
