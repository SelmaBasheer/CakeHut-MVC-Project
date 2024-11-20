using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CakeHut.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductImageSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExtraImages",
                table: "ProductImages",
                newName: "ImageUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "ProductImages",
                newName: "ExtraImages");
        }
    }
}
