﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Serifu.Data.Sqlite;

#nullable disable

namespace Serifu.Data.Sqlite.Migrations
{
    [DbContext(typeof(SerifuDbContext))]
    [Migration("20250216184045_S3ObjectCacheSizeColumn")]
    partial class S3ObjectCacheSizeColumn
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.0");

            modelBuilder.Entity("Serifu.Data.AudioFile", b =>
                {
                    b.Property<string>("ObjectName")
                        .HasColumnType("TEXT")
                        .HasColumnName("name");

                    b.Property<byte[]>("Data")
                        .IsRequired()
                        .HasColumnType("BLOB")
                        .HasColumnName("data");

                    b.Property<long>("DateImported")
                        .HasColumnType("INTEGER")
                        .HasColumnName("mtime");

                    b.Property<int>("Mode")
                        .HasColumnType("INTEGER")
                        .HasColumnName("mode");

                    b.Property<int>("Size")
                        .HasColumnType("INTEGER")
                        .HasColumnName("sz");

                    b.HasKey("ObjectName");

                    b.ToTable("sqlar", (string)null);
                });

            modelBuilder.Entity("Serifu.Data.AudioFileCache", b =>
                {
                    b.Property<string>("OriginalUri")
                        .HasColumnType("TEXT");

                    b.Property<string>("ObjectName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("OriginalUri");

                    b.HasIndex("ObjectName");

                    b.ToTable("AudioFileCache");
                });

            modelBuilder.Entity("Serifu.Data.Quote", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("AlignmentData")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<DateTime>("DateImported")
                        .HasColumnType("TEXT");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.ComplexProperty<Dictionary<string, object>>("English", "Serifu.Data.Quote.English#Translation", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<string>("AudioFile")
                                .HasColumnType("TEXT");

                            b1.Property<string>("Context")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<string>("Notes")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<string>("SpeakerName")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<string>("Text")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<int>("WordCount")
                                .HasColumnType("INTEGER");
                        });

                    b.ComplexProperty<Dictionary<string, object>>("Japanese", "Serifu.Data.Quote.Japanese#Translation", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<string>("AudioFile")
                                .HasColumnType("TEXT");

                            b1.Property<string>("Context")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<string>("Notes")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<string>("SpeakerName")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<string>("Text")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<int>("WordCount")
                                .HasColumnType("INTEGER");
                        });

                    b.HasKey("Id");

                    b.HasIndex("Source");

                    b.ToTable("Quotes");
                });

            modelBuilder.Entity("Serifu.Data.S3ObjectCache", b =>
                {
                    b.Property<string>("Bucket")
                        .HasColumnType("TEXT");

                    b.Property<string>("ObjectName")
                        .HasColumnType("TEXT");

                    b.Property<long>("Size")
                        .HasColumnType("INTEGER");

                    b.HasKey("Bucket", "ObjectName");

                    b.ToTable("S3ObjectCache");
                });

            modelBuilder.Entity("Serifu.Data.AudioFileCache", b =>
                {
                    b.HasOne("Serifu.Data.AudioFile", null)
                        .WithMany()
                        .HasForeignKey("ObjectName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
