using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace compia_bookstore_api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemProductType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "OrderItems",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "OrderItems");
        }
    }
}
