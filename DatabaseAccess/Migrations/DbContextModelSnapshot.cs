﻿// <auto-generated />
using DatabaseAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;

namespace DatabaseAccess.Migrations
{
    [DbContext(typeof(DbContext))]
    partial class DbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.1-rtm-125");

            modelBuilder.Entity("DatabaseAccess.Entities.DbTransaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("BaseAsset");

                    b.Property<string>("Description");

                    b.Property<string>("ExtOrderId");

                    b.Property<string>("QuoteAsset");

                    b.Property<string>("Source");

                    b.Property<string>("SourceAsset");

                    b.Property<decimal>("SourceFee");

                    b.Property<decimal>("SourceSentAmount");

                    b.Property<string>("Target");

                    b.Property<string>("TargetAsset");

                    b.Property<decimal>("TargetFee");

                    b.Property<decimal>("TargetReceivedAmount");

                    b.Property<DateTime>("Timestamp");

                    b.Property<decimal>("UnitPrice");

                    b.HasKey("Id");

                    b.ToTable("Transactions");
                });
#pragma warning restore 612, 618
        }
    }
}
