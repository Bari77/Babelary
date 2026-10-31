using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FilePattern : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePattern",
                table: "Projects",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "{locale}/{name}.json");

            migrationBuilder.Sql(
                """
                UPDATE "Projects" AS project
                SET "FilePattern" = '{locale}.json'
                WHERE (
                    SELECT COUNT(*)
                    FROM "ProjectFiles" AS file
                    WHERE file."ProjectId" = project."Id"
                ) = 1
                AND EXISTS (
                    SELECT 1
                    FROM "ProjectFiles" AS file
                    WHERE file."ProjectId" = project."Id"
                      AND file."Name" = project."SourceLocale" || '.json'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePattern",
                table: "Projects");
        }
    }
}
