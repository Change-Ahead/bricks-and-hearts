﻿// <auto-generated />
using System;
using BricksAndHearts.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BricksAndHearts.Migrations
{
    [DbContext(typeof(BricksAndHeartsDbContext))]
    partial class BricksAndHeartsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
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

                    b.Property<int?>("ApprovalAdminId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("ApprovalTime")
                        .HasColumnType("datetime2");

                    b.Property<bool>("CharterApproved")
                        .HasColumnType("bit");

                    b.Property<string>("CompanyName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("InviteLink")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsLandlordForProfit")
                        .HasColumnType("bit");

                    b.Property<bool>("LandlordProvidedCharterStatus")
                        .HasColumnType("bit");

                    b.Property<string>("LandlordType")
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

                    b.Property<bool?>("AcceptsBenefits")
                        .HasColumnType("bit");

                    b.Property<bool?>("AcceptsCouple")
                        .HasColumnType("bit");

                    b.Property<bool?>("AcceptsFamily")
                        .HasColumnType("bit");

                    b.Property<bool?>("AcceptsNotEET")
                        .HasColumnType("bit");

                    b.Property<bool?>("AcceptsPets")
                        .HasColumnType("bit");

                    b.Property<bool?>("AcceptsSingleTenant")
                        .HasColumnType("bit");

                    b.Property<bool?>("AcceptsWithoutGuarantor")
                        .HasColumnType("bit");

                    b.Property<string>("AddressLine1")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AddressLine2")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AddressLine3")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("County")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("CreationTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsIncomplete")
                        .HasColumnType("bit");

                    b.Property<int>("LandlordId")
                        .HasColumnType("int");

                    b.Property<int?>("NumOfBedrooms")
                        .HasColumnType("int");

                    b.Property<string>("Postcode")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PropertyType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("Rent")
                        .HasColumnType("int");

                    b.Property<string>("TownOrCity")
                        .HasColumnType("nvarchar(max)");

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

                    b.Property<bool>("HasRequestedAdmin")
                        .HasColumnType("bit");

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
