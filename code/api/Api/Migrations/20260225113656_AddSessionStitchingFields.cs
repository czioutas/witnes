using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionStitchingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "metrics_silver",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionRef",
                table: "metrics_silver",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NavigationId",
                table: "metrics_gold",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "metrics_gold",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionRef",
                table: "metrics_gold",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_metrics_silver_GuestId_PageRequestedAtByVisitor",
                table: "metrics_silver",
                columns: new[] { "GuestId", "PageRequestedAtByVisitor" });

            migrationBuilder.CreateIndex(
                name: "IX_metrics_silver_NavigationId",
                table: "metrics_silver",
                column: "NavigationId");

            migrationBuilder.CreateIndex(
                name: "IX_metrics_silver_UserId_PageRequestedAtByVisitor",
                table: "metrics_silver",
                columns: new[] { "UserId", "PageRequestedAtByVisitor" });

            migrationBuilder.CreateIndex(
                name: "IX_metrics_gold_NavigationId",
                table: "metrics_gold",
                column: "NavigationId");

            migrationBuilder.CreateIndex(
                name: "IX_metrics_gold_SessionId",
                table: "metrics_gold",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_metrics_silver_GuestId_PageRequestedAtByVisitor",
                table: "metrics_silver");

            migrationBuilder.DropIndex(
                name: "IX_metrics_silver_NavigationId",
                table: "metrics_silver");

            migrationBuilder.DropIndex(
                name: "IX_metrics_silver_UserId_PageRequestedAtByVisitor",
                table: "metrics_silver");

            migrationBuilder.DropIndex(
                name: "IX_metrics_gold_NavigationId",
                table: "metrics_gold");

            migrationBuilder.DropIndex(
                name: "IX_metrics_gold_SessionId",
                table: "metrics_gold");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "metrics_silver");

            migrationBuilder.DropColumn(
                name: "SessionRef",
                table: "metrics_silver");

            migrationBuilder.DropColumn(
                name: "NavigationId",
                table: "metrics_gold");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "metrics_gold");

            migrationBuilder.DropColumn(
                name: "SessionRef",
                table: "metrics_gold");
        }
    }
}
