﻿// <auto-generated />
using System;
using BricksAndHearts.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BricksAndHearts.Migrations
{
    [DbContext(typeof(BricksAndHeartsDbContext))]
    [Migration("20220719134520_CreatePropertiesTable")]
    partial class CreatePropertiesTable
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("BricksAndHearts.Database.LandlordDbModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("CompanyName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Landlord");
                });

            modelBuilder.Entity("BricksAndHearts.Database.PropertyDbModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("LandlordId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("LandlordId");

                    b.ToTable("Property");
                });

            modelBuilder.Entity("BricksAndHearts.Database.UserDbModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("GoogleAccountId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("GoogleEmail")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GoogleUserName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsAdmin")
                        .HasColumnType("bit");

                    b.Property<int?>("LandlordId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("GoogleAccountId")
                        .IsUnique();

                    b.HasIndex("LandlordId")
                        .IsUnique()
                        .HasFilter("[LandlordId] IS NOT NULL");

                    b.ToTable("User");
                });

            modelBuilder.Entity("BricksAndHearts.Database.PropertyDbModel", b =>
                {
                    b.HasOne("BricksAndHearts.Database.LandlordDbModel", "Landlord")
                        .WithMany("Properties")
                        .HasForeignKey("LandlordId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Landlord");
                });

            modelBuilder.Entity("BricksAndHearts.Database.UserDbModel", b =>
                {
                    b.HasOne("BricksAndHearts.Database.LandlordDbModel", "Landlord")
                        .WithOne("User")
                        .HasForeignKey("BricksAndHearts.Database.UserDbModel", "LandlordId");

                    b.Navigation("Landlord");
                });

            modelBuilder.Entity("BricksAndHearts.Database.LandlordDbModel", b =>
                {
                    b.Navigation("Properties");

                    b.Navigation("User");
                });
#pragma warning restore 612, 618
        }
    }
}
