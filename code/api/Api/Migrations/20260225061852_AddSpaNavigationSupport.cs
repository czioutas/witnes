using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSpaNavigationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "metrics_silver",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NavigationId",
                table: "metrics_silver",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentNavigationId",
                table: "metrics_silver",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "metrics_gold",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ParentNavigationId",
                table: "metrics_gold",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventType",
                table: "metrics_silver");

            migrationBuilder.DropColumn(
                name: "NavigationId",
                table: "metrics_silver");

            migrationBuilder.DropColumn(
                name: "ParentNavigationId",
                table: "metrics_silver");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "metrics_gold");

            migrationBuilder.DropColumn(
                name: "ParentNavigationId",
                table: "metrics_gold");
        }
    }
}
