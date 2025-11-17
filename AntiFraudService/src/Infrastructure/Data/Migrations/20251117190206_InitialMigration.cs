using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntiFraudService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "approved_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_external_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    occurred_on_date = table.Column<DateOnly>(type: "date", nullable: false),
                    occurred_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approved_transactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transaction_evaluations",
                columns: table => new
                {
                    transaction_evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_external_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    evaluated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_evaluations", x => x.transaction_evaluation_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_approved_transactions_source_account_id_occurred_on_date",
                table: "approved_transactions",
                columns: new[] { "source_account_id", "occurred_on_date" });

            migrationBuilder.CreateIndex(
                name: "IX_approved_transactions_transaction_external_id",
                table: "approved_transactions",
                column: "transaction_external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_evaluations_transaction_external_id",
                table: "transaction_evaluations",
                column: "transaction_external_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approved_transactions");

            migrationBuilder.DropTable(
                name: "transaction_evaluations");
        }
    }
}
