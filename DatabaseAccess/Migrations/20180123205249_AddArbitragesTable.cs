using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace DatabaseAccess.Migrations
{
    public partial class AddArbitragesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArbitrageId",
                table: "Transactions",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Arbitrages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BaseAsset = table.Column<string>(nullable: true),
                    BuyingExchange = table.Column<string>(nullable: true),
                    IsBaseCurrencyBalanceSufficient = table.Column<bool>(nullable: false),
                    IsProfitable = table.Column<bool>(nullable: false),
                    IsQuoteCurrencyBalanceSufficient = table.Column<bool>(nullable: false),
                    QuoteAsset = table.Column<string>(nullable: true),
                    SellingExchange = table.Column<string>(nullable: true),
                    EstimatedAvgNegativeSpreadPercentage_PercentageRatio = table.Column<decimal>(nullable: false),
                    MaxNegativeSpreadPercentage_PercentageRatio = table.Column<decimal>(nullable: false),
                    MaxProfitPercentage_PercentageRatio = table.Column<decimal>(nullable: false),
                    BaseCurrencyBalance_Asset = table.Column<string>(nullable: true),
                    BaseCurrencyBalance_Value = table.Column<decimal>(nullable: false),
                    BestBuyPrice_Asset = table.Column<string>(nullable: true),
                    BestBuyPrice_Value = table.Column<decimal>(nullable: false),
                    BestSellPrice_Asset = table.Column<string>(nullable: true),
                    BestSellPrice_Value = table.Column<decimal>(nullable: false),
                    BuyLimitPricePerUnit_Asset = table.Column<string>(nullable: true),
                    BuyLimitPricePerUnit_Value = table.Column<decimal>(nullable: false),
                    EstimatedAvgBuyUnitPrice_Asset = table.Column<string>(nullable: true),
                    EstimatedAvgBuyUnitPrice_Value = table.Column<decimal>(nullable: false),
                    EstimatedAvgNegativeSpread_Asset = table.Column<string>(nullable: true),
                    EstimatedAvgNegativeSpread_Value = table.Column<decimal>(nullable: false),
                    EstimatedAvgSellUnitPrice_Asset = table.Column<string>(nullable: true),
                    EstimatedAvgSellUnitPrice_Value = table.Column<decimal>(nullable: false),
                    MaxBaseCurrencyAmountToArbitrage_Asset = table.Column<string>(nullable: true),
                    MaxBaseCurrencyAmountToArbitrage_Value = table.Column<decimal>(nullable: false),
                    MaxBuyFee_Asset = table.Column<string>(nullable: true),
                    MaxBuyFee_Value = table.Column<decimal>(nullable: false),
                    MaxNegativeSpread_Asset = table.Column<string>(nullable: true),
                    MaxNegativeSpread_Value = table.Column<decimal>(nullable: false),
                    MaxQuoteCurrencyAmountToSpend_Asset = table.Column<string>(nullable: true),
                    MaxQuoteCurrencyAmountToSpend_Value = table.Column<decimal>(nullable: false),
                    MaxQuoteCurrencyProfit_Asset = table.Column<string>(nullable: true),
                    MaxQuoteCurrencyProfit_Value = table.Column<decimal>(nullable: false),
                    MaxQuoteCurrencyToEarn_Asset = table.Column<string>(nullable: true),
                    MaxQuoteCurrencyToEarn_Value = table.Column<decimal>(nullable: false),
                    MaxSellFee_Asset = table.Column<string>(nullable: true),
                    MaxSellFee_Value = table.Column<decimal>(nullable: false),
                    QuoteCurrencyBalance_Asset = table.Column<string>(nullable: true),
                    QuoteCurrencyBalance_Value = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arbitrages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ArbitrageId",
                table: "Transactions",
                column: "ArbitrageId");

            /* migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Arbitrages_ArbitrageId",
                table: "Transactions",
                column: "ArbitrageId",
                principalTable: "Arbitrages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict); */
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Arbitrages_ArbitrageId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "Arbitrages");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ArbitrageId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ArbitrageId",
                table: "Transactions");
        }
    }
}
