using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Memberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectInvites_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectInviteLocales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InviteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectInviteLocales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectInviteLocales_ProjectInvites_InviteId",
                        column: x => x.InviteId,
                        principalTable: "ProjectInvites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMemberLocales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMemberLocales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMemberLocales_ProjectMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "ProjectMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInviteLocales_InviteId_Locale",
                table: "ProjectInviteLocales",
                columns: new[] { "InviteId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvites_ProjectId",
                table: "ProjectInvites",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvites_TokenHash",
                table: "ProjectInvites",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMemberLocales_MemberId_Locale",
                table: "ProjectMemberLocales",
                columns: new[] { "MemberId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_AccountId",
                table: "ProjectMembers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId_AccountId",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "AccountId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectInviteLocales");

            migrationBuilder.DropTable(
                name: "ProjectMemberLocales");

            migrationBuilder.DropTable(
                name: "ProjectInvites");

            migrationBuilder.DropTable(
                name: "ProjectMembers");
        }
    }
}
