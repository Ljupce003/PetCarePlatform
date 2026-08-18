using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TreatmentAndNotificationService.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TreatmentDbContext))]
[Migration("20260817090000_AddVeterinarianNotificationRecipient")]
public partial class AddVeterinarianNotificationRecipient : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "VeterinarianId",
            table: "notifications",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_notifications_VeterinarianId",
            table: "notifications",
            column: "VeterinarianId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_notifications_VeterinarianId", table: "notifications");
        migrationBuilder.DropColumn(name: "VeterinarianId", table: "notifications");
    }
}
