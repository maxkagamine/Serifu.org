// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

namespace Serifu.Data;

/// <summary>
/// A word alignment mapping a span of the "from" text to its translation in the "to" text.
/// </summary>
/// <param name="FromStart">The inclusive start index of the "from" text.</param>
/// <param name="FromEnd">The exclusive end index of the "from" text.</param>
/// <param name="ToStart">The inclusive start index of the "to" text.</param>
/// <param name="ToEnd">The exclusive end index of the "to" text.</param>
public readonly record struct Alignment(ushort FromStart, ushort FromEnd, ushort ToStart, ushort ToEnd)
    : IComparable<Alignment>
{
    public int CompareTo(Alignment other) =>
        FromStart != other.FromStart ? FromStart - other.FromStart :
        FromEnd != other.FromEnd ? FromEnd - other.FromEnd :
        ToStart != other.ToStart ? ToStart - other.ToStart :
        ToEnd - other.ToEnd;
}
