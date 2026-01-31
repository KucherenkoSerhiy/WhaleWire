using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhaleWire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBalanceToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "balance",
                table: "monitored_addresses",
                type: "text",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "balance",
                table: "monitored_addresses",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
