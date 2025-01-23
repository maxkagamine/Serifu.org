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

using System.Security.Cryptography;
using System.Text;

namespace Serifu.Web.Helpers;

/// <summary>
/// Amazon provides an "AWSSDK.Extensions.CloudFront.Signers" package for this, but it's rather inefficient. My code is
/// 6-12% faster and consumes 95% less memory (benchmarked with batches of 10/100/1000, for loop or parallel).
/// </summary>
internal class CloudFrontUrlSigner : IUrlSigner
{
    private readonly string keyPairId;
    private readonly RSAParameters rsaParameters;

    public CloudFrontUrlSigner(string rsaKeyPath, string keyPairId)
    {
        this.keyPairId = keyPairId;
        rsaParameters = ReadRsaParameters(rsaKeyPath);
    }

    public string SignUrl(string url, DateTime expires)
    {
        using var rsa = RSA.Create(rsaParameters);

        long timestamp = ((DateTimeOffset)expires).ToUnixTimeSeconds();
        string policy =
            $"{{\"Statement\":[{{\"Resource\":\"{url}\",\"Condition\":{{\"DateLessThan\":{{\"AWS:EpochTime\":{timestamp}}}}}}}]}}";

        // Total stack allocation:
        // 1024-bit key = 320 bytes, 2048-bit key = 620 bytes, 4096-bit key = 1216 bytes

        Span<byte> hash = stackalloc byte[SHA1.HashSizeInBytes];
        SHA1.HashData(Encoding.UTF8.GetBytes(policy), hash);

        Span<byte> signature = stackalloc byte[rsaParameters.Modulus!.Length];
        signature = signature[..rsa.SignHash(hash, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1)];

        Span<char> urlSafeSignature = stackalloc char[(signature.Length + 2) / 3 * 4];
        Convert.TryToBase64Chars(signature, urlSafeSignature, out int charsWritten);
        urlSafeSignature = urlSafeSignature[..charsWritten];
        urlSafeSignature.Replace('+', '-');
        urlSafeSignature.Replace('=', '_');
        urlSafeSignature.Replace('/', '~');

        string signedUrl =
            $"{url}{(url.Contains('?') ? "&" : "?")}Expires={timestamp}&Signature={urlSafeSignature}&Key-Pair-Id={keyPairId}";

        return signedUrl;
    }

    private static RSAParameters ReadRsaParameters(string rsaKeyPath)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(rsaKeyPath));
        return rsa.ExportParameters(true);
    }
}
