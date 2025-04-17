﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenAutomate.Infrastructure.DbContext;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.Authority", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("LastModifyAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("LastModifyBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<Guid>("OrganizationUnitId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("OrganizationUnitId");

                    b.ToTable("Authorities", (string)null);
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.AuthorityResource", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AuthorityId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("OrganizationUnitId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Permission")
                        .HasColumnType("int");

                    b.Property<string>("ResourceName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("OrganizationUnitId");

                    b.HasIndex("AuthorityId", "ResourceName");

                    b.ToTable("AuthorityResources", (string)null);
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.AutomationPackage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastModifyAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("LastModifyBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<Guid>("OrganizationUnitId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("Name");

                    b.HasIndex("OrganizationUnitId");

                    b.ToTable("AutomationPackages", (string)null);
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.BotAgent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("IpAddress")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("LastHeartbeat")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("LastModifyAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("LastModifyBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("MachineKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MachineName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<Guid>("OrganizationUnitId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("RegisteredAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("MachineName");

                    b.HasIndex("OrganizationUnitId");

                    b.HasIndex("Status");

                    b.ToTable("BotAgents", (string)null);
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.Execution", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("BotAgentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("EndTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("ErrorMessage")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("LastModifyAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("LastModifyBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("LogOutput")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid?>("OrganizationUnitId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("PackageId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("ScheduleId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("BotAgentId");

                    b.HasIndex("OrganizationUnitId");

                    b.HasIndex("PackageId");

                    b.HasIndex("ScheduleId");

                    b.HasIndex("StartTime");

                    b.HasIndex("Status");

                    b.ToTable("Executions", (string)null);
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.OrganizationUnit", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastModifyAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("LastModifyBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("OrganizationUnits", (string)null);
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.OrganizationUnitUser", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("OrganizationUnitId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("UserId", "OrganizationUnitId");

                    b.HasIndex("OrganizationUnitId");

                    b.ToTable("OrganizationUnitUsers", (string)null);
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.PackageVersion", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastModifyAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("LastModifyBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("PackageId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("VersionNumber")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("PackageId", "VersionNumber")
                        .IsUnique();

                    b.ToTable("PackageVersions", (string)null);
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.RefreshToken", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CreatedByIp")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("Expires")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("LastModifyAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("LastModifyBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ReasonRevoked")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ReplacedByToken")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("Revoked")
                        .HasColumnType("datetime2");

                    b.Property<string>("RevokedByIp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Token")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("Token");

                    b.HasIndex("UserId");

                    b.ToTable("RefreshTokens", (string)null);
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.Schedule", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("CreatedById")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CronExpression")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastModifyAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("LastModifyBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("OrganizationUnitId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("PackageId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("CreatedById");

                    b.HasIndex("IsActive");

                    b.HasIndex("OrganizationUnitId");

                    b.HasIndex("PackageId");

                    b.ToTable("Schedules", (string)null);
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("LastModifyAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid?>("LastModifyBy")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("LastName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Login")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PasswordSalt")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Users", (string)null);
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.UserAuthority", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AuthorityId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("OrganizationUnitId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("UserId", "AuthorityId");

                    b.HasIndex("AuthorityId");

                    b.HasIndex("OrganizationUnitId");

                    b.HasIndex("UserId", "AuthorityId")
                        .IsUnique();

                    b.ToTable("UserAuthorities", (string)null);
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.Authority", b =>
                {
                    b.HasOne("OpenAutomate.Core.Domain.Entities.OrganizationUnit", "OrganizationUnit")
                        .WithMany()
                        .HasForeignKey("OrganizationUnitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("OrganizationUnit");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.AuthorityResource", b =>
                {
                    b.HasOne("OpenAutomate.Core.Domain.Entities.Authority", "Authority")
                        .WithMany("AuthorityResources")
                        .HasForeignKey("AuthorityId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("OpenAutomate.Core.Domain.Entities.OrganizationUnit", "OrganizationUnit")
                        .WithMany()
                        .HasForeignKey("OrganizationUnitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Authority");

                    b.Navigation("OrganizationUnit");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.AutomationPackage", b =>
                {
                    b.HasOne("OpenAutomate.Core.Domain.Entities.User", "Creator")
                        .WithMany()
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("OpenAutomate.Core.Domain.Entities.OrganizationUnit", "OrganizationUnit")
                        .WithMany("AutomationPackages")
                        .HasForeignKey("OrganizationUnitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Creator");

                    b.Navigation("OrganizationUnit");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.BotAgent", b =>
                {
                    b.HasOne("OpenAutomate.Core.Domain.Entities.User", "Owner")
                        .WithMany()
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("OpenAutomate.Core.Domain.Entities.OrganizationUnit", "OrganizationUnit")
                        .WithMany("BotAgents")
                        .HasForeignKey("OrganizationUnitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("OrganizationUnit");

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.Execution", b =>
                {
                    b.HasOne("OpenAutomate.Core.Domain.Entities.BotAgent", "BotAgent")
                        .WithMany("Executions")
                        .HasForeignKey("BotAgentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("OpenAutomate.Core.Domain.Entities.OrganizationUnit", null)
                        .WithMany("Executions")
                        .HasForeignKey("OrganizationUnitId");

                    b.HasOne("OpenAutomate.Core.Domain.Entities.AutomationPackage", "Package")
                        .WithMany("Executions")
                        .HasForeignKey("PackageId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("OpenAutomate.Core.Domain.Entities.Schedule", "Schedule")
                        .WithMany("Executions")
                        .HasForeignKey("ScheduleId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("BotAgent");

                    b.Navigation("Package");

                    b.Navigation("Schedule");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.OrganizationUnitUser", b =>
                {
                    b.HasOne("OpenAutomate.Core.Domain.Entities.OrganizationUnit", "OrganizationUnit")
                        .WithMany("OrganizationUnitUsers")
                        .HasForeignKey("OrganizationUnitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenAutomate.Core.Domain.Entities.User", "User")
                        .WithMany("OrganizationUnitUsers")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("OrganizationUnit");

                    b.Navigation("User");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.PackageVersion", b =>
                {
                    b.HasOne("OpenAutomate.Core.Domain.Entities.AutomationPackage", "Package")
                        .WithMany("Versions")
                        .HasForeignKey("PackageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Package");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.RefreshToken", b =>
                {
                    b.HasOne("OpenAutomate.Core.Domain.Entities.User", "User")
                        .WithMany("RefreshTokens")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.Schedule", b =>
                {
                    b.HasOne("OpenAutomate.Core.Domain.Entities.User", "CreatedBy")
                        .WithMany()
                        .HasForeignKey("CreatedById")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("OpenAutomate.Core.Domain.Entities.OrganizationUnit", null)
                        .WithMany("Schedules")
                        .HasForeignKey("OrganizationUnitId");

                    b.HasOne("OpenAutomate.Core.Domain.Entities.AutomationPackage", "Package")
                        .WithMany("Schedules")
                        .HasForeignKey("PackageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CreatedBy");

                    b.Navigation("Package");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.UserAuthority", b =>
                {
                    b.HasOne("OpenAutomate.Core.Domain.Entities.Authority", "Authority")
                        .WithMany("UserAuthorities")
                        .HasForeignKey("AuthorityId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("OpenAutomate.Core.Domain.Entities.OrganizationUnit", "OrganizationUnit")
                        .WithMany()
                        .HasForeignKey("OrganizationUnitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenAutomate.Core.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Authority");

                    b.Navigation("OrganizationUnit");

                    b.Navigation("User");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.Authority", b =>
                {
                    b.Navigation("AuthorityResources");

                    b.Navigation("UserAuthorities");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.AutomationPackage", b =>
                {
                    b.Navigation("Executions");

                    b.Navigation("Schedules");

                    b.Navigation("Versions");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.BotAgent", b =>
                {
                    b.Navigation("Executions");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.OrganizationUnit", b =>
                {
                    b.Navigation("AutomationPackages");

                    b.Navigation("BotAgents");

                    b.Navigation("Executions");

                    b.Navigation("OrganizationUnitUsers");

                    b.Navigation("Schedules");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.Schedule", b =>
                {
                    b.Navigation("Executions");
                });

            modelBuilder.Entity("OpenAutomate.Core.Domain.Entities.User", b =>
                {
                    b.Navigation("OrganizationUnitUsers");

                    b.Navigation("RefreshTokens");
                });
#pragma warning restore 612, 618
        }
    }
}
