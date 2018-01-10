using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace DatabaseAccess.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BaseAsset = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    ExtOrderId = table.Column<string>(nullable: true),
                    QuoteAsset = table.Column<string>(nullable: true),
                    Source = table.Column<string>(nullable: true),
                    SourceAsset = table.Column<string>(nullable: true),
                    SourceFee = table.Column<decimal>(nullable: false),
                    SourceSentAmount = table.Column<decimal>(nullable: false),
                    Target = table.Column<string>(nullable: true),
                    TargetAsset = table.Column<string>(nullable: true),
                    TargetFee = table.Column<decimal>(nullable: false),
                    TargetReceivedAmount = table.Column<decimal>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    UnitPrice = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");
        }
    }
}
