using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddStateLookupTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create States lookup table
            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.Id);
                });

            // Seed default state/province data (Australia) - only if table is empty
            migrationBuilder.Sql(@"
                INSERT INTO States (Id, Code, Name, IsActive)
                SELECT 1, 'NSW', 'New South Wales', 1
                WHERE NOT EXISTS (SELECT 1 FROM States)
                UNION ALL
                SELECT 2, 'VIC', 'Victoria', 1
                WHERE NOT EXISTS (SELECT 1 FROM States)
                UNION ALL
                SELECT 3, 'QLD', 'Queensland', 1
                WHERE NOT EXISTS (SELECT 1 FROM States)
                UNION ALL
                SELECT 4, 'SA', 'South Australia', 1
                WHERE NOT EXISTS (SELECT 1 FROM States)
                UNION ALL
                SELECT 5, 'WA', 'Western Australia', 1
                WHERE NOT EXISTS (SELECT 1 FROM States)
                UNION ALL
                SELECT 6, 'TAS', 'Tasmania', 1
                WHERE NOT EXISTS (SELECT 1 FROM States)
                UNION ALL
                SELECT 7, 'NT', 'Northern Territory', 1
                WHERE NOT EXISTS (SELECT 1 FROM States)
                UNION ALL
                SELECT 8, 'ACT', 'Australian Capital Territory', 1
                WHERE NOT EXISTS (SELECT 1 FROM States);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "States");
        }
    }
}
