using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260903120000_AllowSameDayParkingAssignmentEnd")]
public class AllowSameDayParkingAssignmentEnd : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("chk_parking_assignments_dates", "parking_assignments");
        migrationBuilder.AddCheckConstraint("chk_parking_assignments_dates", "parking_assignments",
            "assigned_to IS NULL OR assigned_to >= assigned_from");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally fails if same-day history exists; never rewrite historical dates.
        migrationBuilder.DropCheckConstraint("chk_parking_assignments_dates", "parking_assignments");
        migrationBuilder.AddCheckConstraint("chk_parking_assignments_dates", "parking_assignments",
            "assigned_to IS NULL OR assigned_to > assigned_from");
    }
}
