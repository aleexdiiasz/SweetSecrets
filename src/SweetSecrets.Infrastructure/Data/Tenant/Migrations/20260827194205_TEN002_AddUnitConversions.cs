using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SweetSecrets.Infrastructure.Data.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class TEN002_AddUnitConversions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConversionFactor",
                table: "units",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MeasurementType",
                table: "units",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE units
                SET
                    "MeasurementType" =
                        CASE "Code"
                            WHEN 'GR'  THEN 1
                            WHEN 'KG'  THEN 1
                            WHEN 'ML'  THEN 2
                            WHEN 'L'   THEN 2
                            WHEN 'PZA' THEN 3
                        END,
                    "ConversionFactor" =
                        CASE "Code"
                            WHEN 'GR'  THEN 1
                            WHEN 'KG'  THEN 1000
                            WHEN 'ML'  THEN 1
                            WHEN 'L'   THEN 1000
                            WHEN 'PZA' THEN 1
                        END
                WHERE "Code" IN ('GR', 'KG', 'ML', 'L', 'PZA');
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "ConversionFactor",
                table: "units",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldPrecision: 18,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MeasurementType",
                table: "units",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConversionFactor",
                table: "units");

            migrationBuilder.DropColumn(
                name: "MeasurementType",
                table: "units");
        }
    }
}