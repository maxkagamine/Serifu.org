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
/// Used to keep track of files uploaded to S3 (to avoid slow and expensive LIST requests).
/// </summary>
/// <param name="Bucket">The S3 bucket name.</param>
/// <param name="ObjectName">The object name within the bucket.</param>
/// <param name="Size">Size of the object in bytes.</param>
public record S3ObjectCache(string Bucket, string ObjectName, long Size);
