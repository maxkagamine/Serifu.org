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
    [Migration("20240615092544_SqlarTable")]
    partial class SqlarTable
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

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
#pragma warning restore 612, 618
        }
    }
}
