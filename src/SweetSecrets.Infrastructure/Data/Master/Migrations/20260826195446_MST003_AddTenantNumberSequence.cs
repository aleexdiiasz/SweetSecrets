using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SweetSecrets.Infrastructure.Data.Master.Migrations
{
    /// <inheritdoc />
    public partial class MST003_AddTenantNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "tenant_number_seq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "tenant_number_seq");
        }
    }
}
