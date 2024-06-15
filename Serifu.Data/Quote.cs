﻿namespace Serifu.Data;

public record Quote
{
    /// <summary>
    /// ID generated by <see cref="QuoteId"/> that will refer to this quote even if the db is cleared and reimported.
    /// </summary>
    public required long Id { get; init; }

    /// <summary>
    /// The game from which this quote originates, and the importer responsible for it.
    /// </summary>
    public required Source Source { get; init; }

    /// <summary>
    /// The quote's English translation.
    /// </summary>
    public required Translation English { get; init; }

    /// <summary>
    /// The quote's Japanese translation.
    /// </summary>
    public required Translation Japanese { get; init; }

    /// <summary>
    /// The date this quote was imported. May be used to determine if quotes need to be updated.
    /// </summary>
    public DateTime DateImported { get; init; } = DateTime.Now;
}
