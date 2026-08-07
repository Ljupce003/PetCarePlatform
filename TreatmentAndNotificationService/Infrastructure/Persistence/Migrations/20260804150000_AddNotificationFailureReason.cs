using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TreatmentAndNotificationService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TreatmentDbContext))]
[Migration("20260804150000_AddNotificationFailureReason")]
public partial class AddNotificationFailureReason : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FailureReason",
            table: "notifications",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "FailureReason", table: "notifications");
    }
}
