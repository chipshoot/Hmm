using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hmm.Core.Dal.EF.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:note_content_format_type.note_content_format_type", "plain_text,xml,json,markdown");

            migrationBuilder.CreateTable(
                name: "contacts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contact = table.Column<string>(type: "text", nullable: false),
                    isactivated = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notecatalogs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    schema = table.Column<string>(type: "xml", nullable: false),
                    format = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    isdefault = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notecatalogs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    isactivated = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "authors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accountname = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    contactinfo = table.Column<int>(type: "integer", nullable: true),
                    role = table.Column<int>(type: "integer", nullable: false),
                    isactivated = table.Column<bool>(type: "boolean", nullable: false),
                    bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    avatarurl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authors", x => x.id);
                    table.ForeignKey(
                        name: "FK_authors_contacts_contactinfo",
                        column: x => x.contactinfo,
                        principalTable: "contacts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    catalogid = table.Column<int>(type: "integer", nullable: false),
                    authorid = table.Column<int>(type: "integer", nullable: false),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: false),
                    createdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lastmodifieddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    createdby = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    lastmodifiedby = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ts = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_notes_authors",
                        column: x => x.authorid,
                        principalTable: "authors",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_notes_catalogs",
                        column: x => x.catalogid,
                        principalTable: "notecatalogs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notetagrefs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    noteid = table.Column<int>(type: "integer", nullable: false),
                    tagid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notetagrefs", x => x.id);
                    table.ForeignKey(
                        name: "fk_notetagrefs_notes",
                        column: x => x.noteid,
                        principalTable: "notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_notetagrefs_tags",
                        column: x => x.tagid,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_authors_contactinfo",
                table: "authors",
                column: "contactinfo");

            migrationBuilder.CreateIndex(
                name: "uq_authors_accountname",
                table: "authors",
                column: "accountname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_notecatalogs_name",
                table: "notecatalogs",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notes_authorid",
                table: "notes",
                column: "authorid");

            migrationBuilder.CreateIndex(
                name: "ix_notes_catalogid",
                table: "notes",
                column: "catalogid");

            migrationBuilder.CreateIndex(
                name: "ix_notetagrefs_noteid",
                table: "notetagrefs",
                column: "noteid");

            migrationBuilder.CreateIndex(
                name: "ix_notetagrefs_tagid",
                table: "notetagrefs",
                column: "tagid");

            migrationBuilder.CreateIndex(
                name: "uq_tags_name",
                table: "tags",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notetagrefs");

            migrationBuilder.DropTable(
                name: "notes");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "authors");

            migrationBuilder.DropTable(
                name: "notecatalogs");

            migrationBuilder.DropTable(
                name: "contacts");
        }
    }
}
