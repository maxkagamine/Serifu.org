﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Serifu.Data.Local;

#nullable disable

namespace Serifu.Data.Local.Migrations
{
    [DbContext(typeof(QuotesContext))]
    partial class QuotesContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("Serifu.Data.Quote", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("DateImported")
                        .HasColumnType("TEXT");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Source");

                    b.ToTable("Quotes");
                });

            modelBuilder.Entity("Serifu.Data.Translation", b =>
                {
                    b.Property<long>("QuoteId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Language")
                        .HasColumnType("TEXT");

                    b.Property<string>("Context")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Notes")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SpeakerName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("QuoteId", "Language");

                    b.ToTable("Translations", (string)null);
                });

            modelBuilder.Entity("Serifu.Data.Translation", b =>
                {
                    b.HasOne("Serifu.Data.Quote", null)
                        .WithMany("Translations")
                        .HasForeignKey("QuoteId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("Serifu.Data.AudioFile", "AudioFile", b1 =>
                        {
                            b1.Property<long>("TranslationQuoteId")
                                .HasColumnType("INTEGER");

                            b1.Property<string>("TranslationLanguage")
                                .HasColumnType("TEXT");

                            b1.Property<DateTime?>("LastModified")
                                .HasColumnType("TEXT");

                            b1.Property<string>("OriginalName")
                                .HasColumnType("TEXT");

                            b1.Property<string>("Path")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.HasKey("TranslationQuoteId", "TranslationLanguage");

                            b1.ToTable("Translations");

                            b1.WithOwner()
                                .HasForeignKey("TranslationQuoteId", "TranslationLanguage");
                        });

                    b.Navigation("AudioFile");
                });

            modelBuilder.Entity("Serifu.Data.Quote", b =>
                {
                    b.Navigation("Translations");
                });
#pragma warning restore 612, 618
        }
    }
}
