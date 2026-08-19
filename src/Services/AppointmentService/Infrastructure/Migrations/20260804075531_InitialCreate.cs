using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clinics",
                columns: table => new
                {
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinics", x => x.ClinicId);
                });

            migrationBuilder.CreateTable(
                name: "veterinarians",
                columns: table => new
                {
                    VeterinarianId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Specialization = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LicenseNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_veterinarians", x => x.VeterinarianId);
                    table.ForeignKey(
                        name: "FK_veterinarians_clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "clinics",
                        principalColumn: "ClinicId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "availability_slots",
                columns: table => new
                {
                    AvailabilitySlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    VeterinarianId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsBooked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_slots", x => x.AvailabilitySlotId);
                    table.ForeignKey(
                        name: "FK_availability_slots_veterinarians_VeterinarianId",
                        column: x => x.VeterinarianId,
                        principalTable: "veterinarians",
                        principalColumn: "VeterinarianId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PetId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    VeterinarianId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailabilitySlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.AppointmentId);
                    table.ForeignKey(
                        name: "FK_appointments_availability_slots_AvailabilitySlotId",
                        column: x => x.AvailabilitySlotId,
                        principalTable: "availability_slots",
                        principalColumn: "AvailabilitySlotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_AvailabilitySlotId",
                table: "appointments",
                column: "AvailabilitySlotId");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_OwnerId",
                table: "appointments",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_PetId",
                table: "appointments",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_availability_slots_VeterinarianId_StartsAtUtc",
                table: "availability_slots",
                columns: new[] { "VeterinarianId", "StartsAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_veterinarians_ClinicId",
                table: "veterinarians",
                column: "ClinicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "availability_slots");

            migrationBuilder.DropTable(
                name: "veterinarians");

            migrationBuilder.DropTable(
                name: "clinics");
        }
    }
}
