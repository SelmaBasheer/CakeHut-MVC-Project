using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CakeHut.Migrations
{
    /// <inheritdoc />
    public partial class RenameOfferNameToOfferCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OfferName",
                table: "Offers",
                newName: "OfferCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OfferCode",
                table: "Offers",
                newName: "OfferName");
        }
    }
}
