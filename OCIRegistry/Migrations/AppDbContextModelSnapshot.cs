﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OCIRegistry.Data;

#nullable disable

namespace OCIRegistry.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.3");

            modelBuilder.Entity("BlobManifest", b =>
                {
                    b.Property<string>("BlobsId")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("ManifestsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("BlobsId", "ManifestsId");

                    b.HasIndex("ManifestsId");

                    b.ToTable("BlobManifest");
                });

            modelBuilder.Entity("BlobRepository", b =>
                {
                    b.Property<string>("BlobsId")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("RepositoriesId")
                        .HasColumnType("INTEGER");

                    b.HasKey("BlobsId", "RepositoriesId");

                    b.HasIndex("RepositoriesId");

                    b.ToTable("BlobRepository");
                });

            modelBuilder.Entity("OCIRegistry.Models.Database.Blob", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("Size")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Blobs");
                });

            modelBuilder.Entity("OCIRegistry.Models.Database.Manifest", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Content")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<string>("Digest")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("RepositoryId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("RepositoryId");

                    b.ToTable("Manifests");
                });

            modelBuilder.Entity("OCIRegistry.Models.Database.Permission", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte>("Action")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Resource")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong?>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Permissions");
                });

            modelBuilder.Entity("OCIRegistry.Models.Database.Repository", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Repositories");
                });

            modelBuilder.Entity("OCIRegistry.Models.Database.Tag", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ManifestId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ManifestId");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("OCIRegistry.Models.Database.User", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("BlobManifest", b =>
                {
                    b.HasOne("OCIRegistry.Models.Database.Blob", null)
                        .WithMany()
                        .HasForeignKey("BlobsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OCIRegistry.Models.Database.Manifest", null)
                        .WithMany()
                        .HasForeignKey("ManifestsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BlobRepository", b =>
                {
                    b.HasOne("OCIRegistry.Models.Database.Blob", null)
                        .WithMany()
                        .HasForeignKey("BlobsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OCIRegistry.Models.Database.Repository", null)
                        .WithMany()
                        .HasForeignKey("RepositoriesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OCIRegistry.Models.Database.Manifest", b =>
                {
                    b.HasOne("OCIRegistry.Models.Database.Repository", "Repository")
                        .WithMany("Manifests")
                        .HasForeignKey("RepositoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Repository");
                });

            modelBuilder.Entity("OCIRegistry.Models.Database.Permission", b =>
                {
                    b.HasOne("OCIRegistry.Models.Database.User", "User")
                        .WithMany("Permissions")
                        .HasForeignKey("UserId");

                    b.Navigation("User");
                });

            modelBuilder.Entity("OCIRegistry.Models.Database.Tag", b =>
                {
                    b.HasOne("OCIRegistry.Models.Database.Manifest", "Manifest")
                        .WithMany()
                        .HasForeignKey("ManifestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Manifest");
                });

            modelBuilder.Entity("OCIRegistry.Models.Database.Repository", b =>
                {
                    b.Navigation("Manifests");
                });

            modelBuilder.Entity("OCIRegistry.Models.Database.User", b =>
                {
                    b.Navigation("Permissions");
                });
#pragma warning restore 612, 618
        }
    }
}
