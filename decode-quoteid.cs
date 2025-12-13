#!/usr/bin/env dotnet.exe
#:project Serifu.Data
using Serifu.Data;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: decode-quoteid.cs <id>...");
    Environment.Exit(1);
}

for (int i = 0; i < args.Length; i++)
{
    if (i > 0)
    {
        Console.WriteLine();
    }

    if (!long.TryParse(args[i], out long id))
    {
        Console.WriteLine($"Could not parse \"{args[i]}\".");
        Environment.ExitCode = 1;
        continue;
    }

    Source source = (Source)(id >>> 48);

    Console.WriteLine($"ID: {id}");
    Console.WriteLine($"Source: {source}");

    switch (source)
    {
        case Source.Skyrim:
            DecodeSkyrim(id);
            break;
        case Source.Kancolle:
            DecodeKancolle(id);
            break;
        default:
            DecodeGeneric(id);
            break;
    }
}

static void DecodeSkyrim(long id)
{
    int formId = (int)(id >>> 16);
    short responseNumber = (short)id;

    Console.WriteLine($"Form ID: {formId:X8}");
    Console.WriteLine($"Response #: {responseNumber}");
}

static void DecodeKancolle(long id)
{
    short shipNumber = (short)(id >>> 32);
    int index = (int)id;

    Console.WriteLine($"Ship #: {shipNumber}");
    Console.WriteLine($"Index: {index}");
}

static void DecodeGeneric(long id)
{
    int index = (int)id;

    Console.WriteLine($"Index: {index}");
}
