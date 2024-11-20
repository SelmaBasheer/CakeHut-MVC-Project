using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CakeHut.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationDateToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationDate",
                table: "Orders");
        }
    }
}
