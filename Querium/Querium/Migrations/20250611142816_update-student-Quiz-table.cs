using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Querim.Migrations
{
    /// <inheritdoc />
    public partial class updatestudentQuiztable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UploadId",
                table: "StudentQuizzes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadId",
                table: "StudentQuizzes");
        }
    }
}
