﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Serifu.Data;

#nullable disable

namespace Serifu.Data.Migrations
{
    [DbContext(typeof(QuotesContext))]
    partial class QuotesContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.12");

            modelBuilder.Entity("Serifu.Data.Entities.Quote", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AudioFile")
                        .HasColumnType("TEXT");

                    b.Property<string>("Context")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Notes")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("QuoteEnglish")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("QuoteJapanese")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SpeakerEnglish")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SpeakerJapanese")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Source");

                    b.HasIndex("SpeakerEnglish");

                    b.HasIndex("SpeakerJapanese");

                    b.ToTable("Quotes");
                });
#pragma warning restore 612, 618
        }
    }
}
