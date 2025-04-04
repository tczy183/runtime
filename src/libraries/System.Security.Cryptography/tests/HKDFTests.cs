// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public abstract class HKDFTests
    {
        protected abstract byte[] Extract(HashAlgorithmName hash, int prkLength, byte[] ikm, byte[] salt);
        protected abstract byte[] Expand(HashAlgorithmName hash, byte[] prk, int outputLength, byte[] info);
        protected abstract byte[] DeriveKey(HashAlgorithmName hash, byte[] ikm, int outputLength, byte[] salt, byte[] info);

        internal static bool MD5Supported => !PlatformDetection.IsBrowser && !PlatformDetection.IsAzureLinux;
        internal static bool EmptyKeysSupported => !PlatformDetection.IsAzureLinux;

        [Theory]
        [MemberData(nameof(GetHkdfTestCases))]
        public void ExtractTests(HkdfTestCase test)
        {
            byte[] prk = Extract(test.Hash, test.Prk.Length, test.Ikm, test.Salt);
            Assert.Equal(test.Prk, prk);
        }

        [ConditionalTheory(nameof(MD5Supported))]
        [MemberData(nameof(GetHkdfTestCases))]
        public void ExtractTamperHashTests(HkdfTestCase test)
        {
            byte[] prk = Extract(HashAlgorithmName.MD5, 128 / 8, test.Ikm, test.Salt);
            Assert.NotEqual(test.Prk, prk);
        }

        [Theory]
        [MemberData(nameof(GetHkdfTestCases))]
        public void ExtractTamperIkmTests(HkdfTestCase test)
        {
            byte[] ikm = test.Ikm.ToArray();
            ikm[0] ^= 1;
            byte[] prk = Extract(test.Hash, test.Prk.Length, ikm, test.Salt);
            Assert.NotEqual(test.Prk, prk);
        }

        [Theory]
        [MemberData(nameof(GetHkdfTestCasesWithNonEmptySalt))]
        public void ExtractTamperSaltTests(HkdfTestCase test)
        {
            byte[] salt = test.Salt.ToArray();
            salt[0] ^= 1;
            byte[] prk = Extract(test.Hash, test.Prk.Length, test.Ikm, salt);
            Assert.NotEqual(test.Prk, prk);
        }

        [Fact]
        public void ExtractDefaultHash()
        {
            byte[] ikm = new byte[20];
            byte[] salt = new byte[20];
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "hashAlgorithmName",
                () => Extract(default(HashAlgorithmName), 20, ikm, salt));
        }

        [Fact]
        public void ExtractNonsensicalHash()
        {
            byte[] ikm = new byte[20];
            byte[] salt = new byte[20];
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "hashAlgorithmName",
                () => Extract(new HashAlgorithmName("foo"), 20, ikm, salt));
        }

        [ConditionalFact(nameof(EmptyKeysSupported))]
        public void ExtractEmptyIkm()
        {
            byte[] salt = new byte[20];
            byte[] ikm = Array.Empty<byte>();

            // Ensure does not throw
            byte[] prk = Extract(HashAlgorithmName.SHA1, 20, ikm, salt);
            Assert.Equal("FBDB1D1B18AA6C08324B7D64B71FB76370690E1D", prk.ByteArrayToHex());
        }

        [Fact]
        public void ExtractEmptySalt()
        {
            byte[] ikm = new byte[20];
            byte[] salt = Array.Empty<byte>();
            byte[] prk = Extract(HashAlgorithmName.SHA1, 20, ikm, salt);
            Assert.Equal("A3CBF4A40F51A53E046F07397E52DF9286AE93A2", prk.ByteArrayToHex());
        }

        [Theory]
        [MemberData(nameof(GetHkdfTestCases))]
        public void ExpandTests(HkdfTestCase test)
        {
            byte[] okm = Expand(test.Hash, test.Prk, test.Okm.Length, test.Info);
            Assert.Equal(test.Okm, okm);
        }

        [Fact]
        public void ExpandDefaultHash()
        {
            byte[] prk = new byte[20];
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "hashAlgorithmName",
                () => Expand(default(HashAlgorithmName), prk, 20, null));
        }

        [Fact]
        public void ExpandNonsensicalHash()
        {
            byte[] prk = new byte[20];
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "hashAlgorithmName",
                () => Expand(new HashAlgorithmName("foo"), prk, 20, null));
        }

        [Theory]
        [MemberData(nameof(GetHkdfTestCases))]
        public void ExpandTamperPrkTests(HkdfTestCase test)
        {
            byte[] prk = test.Prk.ToArray();
            prk[0] ^= 1;
            byte[] okm = Expand(test.Hash, prk, test.Okm.Length, test.Info);
            Assert.NotEqual(test.Okm, okm);
        }

        [Theory]
        [MemberData(nameof(GetPrkTooShortTestCases))]
        public void ExpandPrkTooShort(HashAlgorithmName hash, int prkSize)
        {
            byte[] prk = new byte[prkSize];
            AssertExtensions.Throws<ArgumentException>(
                "prk",
                () => Expand(hash, prk, 17, Array.Empty<byte>()));
        }

        [Fact]
        public void ExpandOkmMaxSize()
        {
            byte[] prk = new byte[20];

            // Does not throw
            byte[] okm = Expand(HashAlgorithmName.SHA1, prk, 20 * 255, Array.Empty<byte>());
            Assert.Equal(20 * 255, okm.Length);
        }

        [Theory]
        [MemberData(nameof(GetHkdfTestCases))]
        public void DeriveKeyTests(HkdfTestCase test)
        {
            byte[] okm = DeriveKey(test.Hash, test.Ikm, test.Okm.Length, test.Salt, test.Info);
            Assert.Equal(test.Okm, okm);
        }

        [Fact]
        public void DeriveKeyDefaultHash()
        {
            byte[] ikm = new byte[20];
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "hashAlgorithmName",
                () => DeriveKey(default(HashAlgorithmName), ikm, 20, Array.Empty<byte>(), Array.Empty<byte>()));
        }

        [Fact]
        public void DeriveKeyNonSensicalHash()
        {
            byte[] ikm = new byte[20];
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "hashAlgorithmName",
                () => DeriveKey(new HashAlgorithmName("foo"), ikm, 20, Array.Empty<byte>(), Array.Empty<byte>()));
        }

        [Theory]
        [MemberData(nameof(GetHkdfTestCases))]
        public void DeriveKeyTamperIkmTests(HkdfTestCase test)
        {
            byte[] ikm = test.Ikm.ToArray();
            ikm[0] ^= 1;
            byte[] okm = DeriveKey(test.Hash, ikm, test.Okm.Length, test.Salt, test.Info);
            Assert.NotEqual(test.Okm, okm);
        }

        [Theory]
        [MemberData(nameof(GetHkdfTestCasesWithNonEmptySalt))]
        public void DeriveKeyTamperSaltTests(HkdfTestCase test)
        {
            byte[] salt = test.Salt.ToArray();
            salt[0] ^= 1;
            byte[] okm = DeriveKey(test.Hash, test.Ikm, test.Okm.Length, salt, test.Info);
            Assert.NotEqual(test.Okm, okm);
        }

        [Theory]
        [MemberData(nameof(GetHkdfTestCasesWithNonEmptyInfo))]
        public void DeriveKeyTamperInfoTests(HkdfTestCase test)
        {
            byte[] info = test.Info.ToArray();
            info[0] ^= 1;
            byte[] okm = DeriveKey(test.Hash, test.Ikm, test.Okm.Length, test.Salt, info);
            Assert.NotEqual(test.Okm, okm);
        }

        [Theory]
        [MemberData(nameof(Sha3TestCases))]
        public void Sha3Tests(HkdfTestCase test)
        {
            if (PlatformDetection.SupportsSha3)
            {
                byte[] okm = DeriveKey(test.Hash, test.Ikm, test.Okm.Length, test.Salt, test.Info);
                Assert.Equal(test.Okm, okm);
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() =>
                    DeriveKey(test.Hash, test.Ikm, test.Okm.Length, test.Salt, test.Info));
            }
        }

        public static IEnumerable<object[]> GetHkdfTestCases()
        {
            foreach (HkdfTestCase test in Rfc5869TestCases)
            {
                yield return new object[] { test };
            }
        }

        public static IEnumerable<object[]> GetHkdfTestCasesWithNonEmptySalt()
        {
            foreach (HkdfTestCase test in Rfc5869TestCases)
            {
                if (test.Salt != null && test.Salt.Length != 0)
                {
                    yield return new object[] { test };
                }
            }
        }

        public static IEnumerable<object[]> GetHkdfTestCasesWithNonEmptyInfo()
        {
            foreach (HkdfTestCase test in Rfc5869TestCases)
            {
                if (test.Info != null && test.Info.Length != 0)
                {
                    yield return new object[] { test };
                }
            }
        }

        public static IEnumerable<object[]> GetPrkTooShortTestCases()
        {
            yield return new object[] { HashAlgorithmName.SHA1, 0 };
            yield return new object[] { HashAlgorithmName.SHA1, 1 };
            yield return new object[] { HashAlgorithmName.SHA1, 160 / 8 - 1 };
            yield return new object[] { HashAlgorithmName.SHA256, 256 / 8 - 1 };
            yield return new object[] { HashAlgorithmName.SHA512, 512 / 8 - 1 };

            if (MD5Supported)
            {
                yield return new object[] { HashAlgorithmName.MD5, 128 / 8 - 1 };
            }

            if (PlatformDetection.SupportsSha3)
            {
                yield return new object[] { HashAlgorithmName.SHA3_256, SHA3_256.HashSizeInBytes - 1 };
                yield return new object[] { HashAlgorithmName.SHA3_384, SHA3_384.HashSizeInBytes - 1 };
                yield return new object[] { HashAlgorithmName.SHA3_512, SHA3_512.HashSizeInBytes - 1 };
            }
        }

        private static HkdfTestCase[] Rfc5869TestCases { get; } = new HkdfTestCase[7]
        {
            new HkdfTestCase()
            {
                Name = "Basic test case with SHA-256",
                Hash = HashAlgorithmName.SHA256,
                Ikm = "0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b".HexToByteArray(),
                Salt = "000102030405060708090a0b0c".HexToByteArray(),
                Info = "f0f1f2f3f4f5f6f7f8f9".HexToByteArray(),
                Prk = (
                    "077709362c2e32df0ddc3f0dc47bba63" +
                    "90b6c73bb50f9c3122ec844ad7c2b3e5").HexToByteArray(),
                Okm = (
                    "3cb25f25faacd57a90434f64d0362f2a" +
                    "2d2d0a90cf1a5a4c5db02d56ecc4c5bf" +
                    "34007208d5b887185865").HexToByteArray(),
            },
            new HkdfTestCase()
            {
                Name = "Test with SHA-256 and longer inputs/outputs",
                Hash = HashAlgorithmName.SHA256,
                Ikm = (
                    "000102030405060708090a0b0c0d0e0f" +
                    "101112131415161718191a1b1c1d1e1f" +
                    "202122232425262728292a2b2c2d2e2f" +
                    "303132333435363738393a3b3c3d3e3f" +
                    "404142434445464748494a4b4c4d4e4f").HexToByteArray(),
                Salt = (
                    "606162636465666768696a6b6c6d6e6f" +
                    "707172737475767778797a7b7c7d7e7f" +
                    "808182838485868788898a8b8c8d8e8f" +
                    "909192939495969798999a9b9c9d9e9f" +
                    "a0a1a2a3a4a5a6a7a8a9aaabacadaeaf").HexToByteArray(),
                Info = (
                    "b0b1b2b3b4b5b6b7b8b9babbbcbdbebf" +
                    "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf" +
                    "d0d1d2d3d4d5d6d7d8d9dadbdcdddedf" +
                    "e0e1e2e3e4e5e6e7e8e9eaebecedeeef" +
                    "f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff").HexToByteArray(),
                Prk = (
                    "06a6b88c5853361a06104c9ceb35b45c" +
                    "ef760014904671014a193f40c15fc244").HexToByteArray(),
                Okm = (
                    "b11e398dc80327a1c8e7f78c596a4934" +
                    "4f012eda2d4efad8a050cc4c19afa97c" +
                    "59045a99cac7827271cb41c65e590e09" +
                    "da3275600c2f09b8367793a9aca3db71" +
                    "cc30c58179ec3e87c14c01d5c1f3434f" +
                    "1d87").HexToByteArray(),
            },
            new HkdfTestCase()
            {
                Name = "Test with SHA-256 and zero-length salt/info",
                Hash = HashAlgorithmName.SHA256,
                Ikm = "0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b".HexToByteArray(),
                Salt = Array.Empty<byte>(),
                Info = Array.Empty<byte>(),
                Prk = (
                    "19ef24a32c717b167f33a91d6f648bdf" +
                    "96596776afdb6377ac434c1c293ccb04").HexToByteArray(),
                Okm = (
                    "8da4e775a563c18f715f802a063c5a31" +
                    "b8a11f5c5ee1879ec3454e5f3c738d2d" +
                    "9d201395faa4b61a96c8").HexToByteArray(),
            },
            new HkdfTestCase()
            {
                Name = "Basic test case with SHA-1",
                Hash = HashAlgorithmName.SHA1,
                Ikm = "0b0b0b0b0b0b0b0b0b0b0b".HexToByteArray(),
                Salt = "000102030405060708090a0b0c".HexToByteArray(),
                Info = "f0f1f2f3f4f5f6f7f8f9".HexToByteArray(),
                Prk = "9b6c18c432a7bf8f0e71c8eb88f4b30baa2ba243".HexToByteArray(),
                Okm = (
                    "085a01ea1b10f36933068b56efa5ad81" +
                    "a4f14b822f5b091568a9cdd4f155fda2" +
                    "c22e422478d305f3f896").HexToByteArray(),
            },
            new HkdfTestCase()
            {
                Name = "Test with SHA-1 and longer inputs/outputs",
                Hash = HashAlgorithmName.SHA1,
                Ikm = (
                    "000102030405060708090a0b0c0d0e0f" +
                    "101112131415161718191a1b1c1d1e1f" +
                    "202122232425262728292a2b2c2d2e2f" +
                    "303132333435363738393a3b3c3d3e3f" +
                    "404142434445464748494a4b4c4d4e4f").HexToByteArray(),
                Salt = (
                    "606162636465666768696a6b6c6d6e6f" +
                    "707172737475767778797a7b7c7d7e7f" +
                    "808182838485868788898a8b8c8d8e8f" +
                    "909192939495969798999a9b9c9d9e9f" +
                    "a0a1a2a3a4a5a6a7a8a9aaabacadaeaf").HexToByteArray(),
                Info = (
                    "b0b1b2b3b4b5b6b7b8b9babbbcbdbebf" +
                    "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf" +
                    "d0d1d2d3d4d5d6d7d8d9dadbdcdddedf" +
                    "e0e1e2e3e4e5e6e7e8e9eaebecedeeef" +
                    "f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff").HexToByteArray(),
                Prk = "8adae09a2a307059478d309b26c4115a224cfaf6".HexToByteArray(),
                Okm = (
                    "0bd770a74d1160f7c9f12cd5912a06eb" +
                    "ff6adcae899d92191fe4305673ba2ffe" +
                    "8fa3f1a4e5ad79f3f334b3b202b2173c" +
                    "486ea37ce3d397ed034c7f9dfeb15c5e" +
                    "927336d0441f4c4300e2cff0d0900b52" +
                    "d3b4").HexToByteArray(),
            },
            new HkdfTestCase()
            {
                Name = "Test with SHA-1 and zero-length salt/info",
                Hash = HashAlgorithmName.SHA1,
                Ikm = "0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b".HexToByteArray(),
                Salt = Array.Empty<byte>(),
                Info = Array.Empty<byte>(),
                Prk = "da8c8a73c7fa77288ec6f5e7c297786aa0d32d01".HexToByteArray(),
                Okm = (
                    "0ac1af7002b3d761d1e55298da9d0506" +
                    "b9ae52057220a306e07b6b87e8df21d0" +
                    "ea00033de03984d34918").HexToByteArray(),
            },
            new HkdfTestCase()
            {
                Name = "Test with SHA-1, salt not provided (defaults to HashLen zero octets), zero-length info",
                Hash = HashAlgorithmName.SHA1,
                Ikm = "0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c".HexToByteArray(),
                Salt = null,
                Info = Array.Empty<byte>(),
                Prk = "2adccada18779e7c2077ad2eb19d3f3e731385dd".HexToByteArray(),
                Okm = (
                    "2c91117204d745f3500d636a62f64f0a" +
                    "b3bae548aa53d423b0d1f27ebba6f5e5" +
                    "673a081d70cce7acfc48").HexToByteArray(),
            },
        };

        public static IEnumerable<object[]> Sha3TestCases
        {
            // These cases were generated from the openssl kdf command.
            // openssl kdf -keylen 8 -kdfopt digest:SHA3-256 -kdfopt hexkey:000102030405060708090A0B0C0D0E0F \
            //     -kdfopt salt:mysalt -kdfopt info:myinfo -binary HKDF | xxd -p
            get
            {
                yield return new object[]
                {
                    new HkdfTestCase
                    {
                        Name = "SHA3-256 with salt and info",
                        Hash = HashAlgorithmName.SHA3_256,
                        Ikm = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F".HexToByteArray(),
                        Salt = "mysalt"u8.ToArray(),
                        Info = "myinfo"u8.ToArray(),
                        Okm = "35bd9d1c75cf7e30".HexToByteArray(),
                    }
                };

                yield return new object[]
                {
                    new HkdfTestCase
                    {
                        Name = "SHA3-384 with salt and info",
                        Hash = HashAlgorithmName.SHA3_384,
                        Ikm = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F".HexToByteArray(),
                        Salt = "mysalt"u8.ToArray(),
                        Info = "myinfo"u8.ToArray(),
                        Okm = "323a8ab50c7190c8".HexToByteArray(),
                    }
                };

                yield return new object[]
                {
                    new HkdfTestCase
                    {
                        Name = "SHA3-512 with salt and info",
                        Hash = HashAlgorithmName.SHA3_512,
                        Ikm = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F".HexToByteArray(),
                        Salt = "mysalt"u8.ToArray(),
                        Info = "myinfo"u8.ToArray(),
                        Okm = "27693b36a489e9f1".HexToByteArray(),
                    }
                };
            }
        }

        public struct HkdfTestCase
        {
            public string Name { get; set; }
            public HashAlgorithmName Hash { get; set; }
            public byte[] Ikm { get; set; }
            public byte[] Salt { get; set; }
            public byte[] Info { get; set; }
            public byte[] Prk { get; set; }
            public byte[] Okm { get; set; }

            public override string ToString() => Name;
        }

        public class HkdfByteArrayTests : HKDFTests
        {
            protected override byte[] Extract(HashAlgorithmName hash, int prkLength, byte[] ikm, byte[] salt)
            {
                return HKDF.Extract(hash, ikm, salt);
            }

            protected override byte[] Expand(HashAlgorithmName hash, byte[] prk, int outputLength, byte[] info)
            {
                return HKDF.Expand(hash, prk, outputLength, info);
            }

            protected override byte[] DeriveKey(HashAlgorithmName hash, byte[] ikm, int outputLength, byte[] salt, byte[] info)
            {
                return HKDF.DeriveKey(hash, ikm, outputLength, salt, info);
            }

            [Fact]
            public void ExtractNullIkm()
            {
                byte[] salt = new byte[20];
                AssertExtensions.Throws<ArgumentNullException>(
                    "ikm",
                    () => HKDF.Extract(HashAlgorithmName.SHA1, null, salt));
            }

            [Fact]
            public void ExpandOkmMaxSizePlusOne()
            {
                byte[] prk = new byte[20];
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "outputLength",
                    () => HKDF.Expand(HashAlgorithmName.SHA1, prk, 20 * 255 + 1, Array.Empty<byte>()));
            }

            [Fact]
            public void ExpandOkmPotentiallyOverflowingValue()
            {
                byte[] prk = new byte[20];
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "outputLength",
                    () => HKDF.Expand(HashAlgorithmName.SHA1, prk, 8421505, Array.Empty<byte>()));
            }

            [Fact]
            public void ExpandOutputLengthZero()
            {
                byte[] prk = new byte[20];
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "outputLength",
                    () => HKDF.Expand(HashAlgorithmName.SHA1, prk, 0, Array.Empty<byte>()));
            }

            [Fact]
            public void ExpandOutputLengthLessThanZero()
            {
                byte[] prk = new byte[20];
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "outputLength",
                    () => HKDF.Expand(HashAlgorithmName.SHA1, prk, -1, Array.Empty<byte>()));
            }

            [Fact]
            public void DeriveKeyNullIkm()
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "ikm",
                    () => HKDF.DeriveKey(HashAlgorithmName.SHA1, null, 20, Array.Empty<byte>(), Array.Empty<byte>()));
            }

            [Fact]
            public void DeriveKeyOkmMaxSizePlusOne()
            {
                byte[] ikm = new byte[20];
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "outputLength",
                    () => HKDF.DeriveKey(HashAlgorithmName.SHA1, ikm, 20 * 255 + 1, Array.Empty<byte>(), Array.Empty<byte>()));
            }

            [Fact]
            public void DeriveKeyOkmPotentiallyOverflowingValue()
            {
                byte[] ikm = new byte[20];
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "outputLength",
                    () => HKDF.DeriveKey(HashAlgorithmName.SHA1, ikm, 8421505, Array.Empty<byte>(), Array.Empty<byte>()));
            }

            [Fact]
            public void DeriveOutputLengthZero()
            {
                byte[] ikm = new byte[20];
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "outputLength",
                    () => HKDF.DeriveKey(HashAlgorithmName.SHA1, ikm, 0, Array.Empty<byte>(), Array.Empty<byte>()));
            }

            [Fact]
            public void DeriveOutputLengthLessThanZero()
            {
                byte[] ikm = new byte[20];
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    "outputLength",
                    () => HKDF.DeriveKey(HashAlgorithmName.SHA1, ikm, -1, Array.Empty<byte>(), Array.Empty<byte>()));
            }
        }

        public class HkdfSpanTests : HKDFTests
        {
            protected override byte[] Extract(HashAlgorithmName hash, int prkLength, byte[] ikm, byte[] salt)
            {
                byte[] prk = new byte[prkLength];
                Assert.Equal(prkLength, HKDF.Extract(hash, ikm, salt, prk));
                return prk;
            }

            protected override byte[] Expand(HashAlgorithmName hash, byte[] prk, int outputLength, byte[] info)
            {
                byte[] output = new byte[outputLength];
                HKDF.Expand(hash, prk, output, info);
                return output;
            }

            protected override byte[] DeriveKey(HashAlgorithmName hash, byte[] ikm, int outputLength, byte[] salt, byte[] info)
            {
                byte[] output = new byte[outputLength];
                HKDF.DeriveKey(hash, ikm, output, salt, info);
                return output;
            }

            [Fact]
            public void ExtractPrkTooLong()
            {
                byte[] prk = new byte[24];

                for (int i = 0; i < 4; i++)
                {
                    prk[20 + i] = (byte)(i + 5);
                }

                byte[] ikm = new byte[20];
                byte[] salt = new byte[20];
                Assert.Equal(20, HKDF.Extract(HashAlgorithmName.SHA1, ikm, salt, prk));
                Assert.Equal("A3CBF4A40F51A53E046F07397E52DF9286AE93A2", prk.AsSpan(0, 20).ByteArrayToHex());

                for (int i = 0; i < 4; i++)
                {
                    // ensure we didn't modify anything further
                    Assert.Equal((byte)(i + 5), prk[20 + i]);
                }
            }

            [Fact]
            public void OkmMaxSizePlusOne()
            {
                byte[] prk = new byte[20];
                byte[] okm = new byte[20 * 255 + 1];
                AssertExtensions.Throws<ArgumentException>(
                    "output",
                    () => HKDF.Expand(HashAlgorithmName.SHA1, prk, okm, Array.Empty<byte>()));
            }

            [Fact]
            public void OkmMaxSizePotentiallyOverflowingValue()
            {
                byte[] prk = new byte[20];
                byte[] okm = new byte[8421505];
                AssertExtensions.Throws<ArgumentException>(
                    "output",
                    () => HKDF.Expand(HashAlgorithmName.SHA1, prk, okm, Array.Empty<byte>()));
            }

            [Fact]
            public void ExpandOutputLengthZero()
            {
                byte[] prk = new byte[20];
                byte[] okm = new byte[0];

                AssertExtensions.Throws<ArgumentException>(
                    "output",
                    () => HKDF.Expand(HashAlgorithmName.SHA1, prk, okm, Array.Empty<byte>()));
            }

            [Fact]
            public void DeriveKeySpanOkmMaxSizePlusOne()
            {
                byte[] ikm = new byte[20];
                byte[] okm = new byte[20 * 255 + 1];
                AssertExtensions.Throws<ArgumentException>(
                    "output",
                    () => HKDF.DeriveKey(HashAlgorithmName.SHA1, ikm, okm, Array.Empty<byte>(), Array.Empty<byte>()));
            }

            [Fact]
            public void DeriveKeySpanOkmPotentiallyOverflowingValue()
            {
                byte[] ikm = new byte[20];
                byte[] okm = new byte[8421505];
                AssertExtensions.Throws<ArgumentException>(
                    "output",
                    () => HKDF.DeriveKey(HashAlgorithmName.SHA1, ikm, okm, Array.Empty<byte>(), Array.Empty<byte>()));
            }

            [Fact]
            public void DeriveKeyOutputLengthZero()
            {
                byte[] ikm = new byte[20];
                byte[] okm = new byte[0];

                AssertExtensions.Throws<ArgumentException>(
                    "output",
                    () => HKDF.DeriveKey(HashAlgorithmName.SHA1, ikm, okm, Array.Empty<byte>(), Array.Empty<byte>()));
            }

            [Theory]
            [InlineData(0, 0)] // Overlap exactly
            [InlineData(0, 10)] // Output +10 offset over ikm
            [InlineData(10, 0)] // ikm +10 offset over output
            [InlineData(10, 20)] // Both offset, output +10 over ikm
            public void ExtractOverlapsPrkOverKeyMaterial(int ikmOffset, int outputOffset)
            {
                ReadOnlySpan<byte> ikm = "0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b".HexToByteArray();
                ReadOnlySpan<byte> salt = "000102030405060708090a0b0c".HexToByteArray();
                byte[] expectedPrk = "077709362c2e32df0ddc3f0dc47bba6390b6c73bb50f9c3122ec844ad7c2b3e5".HexToByteArray();

                int length = Math.Max(ikm.Length, expectedPrk.Length) + Math.Max(ikmOffset, outputOffset);
                Span<byte> buffer = new byte[length];
                Span<byte> ikmBuffer = buffer.Slice(ikmOffset, ikm.Length);
                Span<byte> outputBuffer = buffer.Slice(outputOffset, expectedPrk.Length);
                ikm.CopyTo(ikmBuffer);

                HKDF.Extract(HashAlgorithmName.SHA256, ikmBuffer, salt, outputBuffer);
                Assert.Equal(expectedPrk, outputBuffer.ToArray());
            }

            [Theory]
            [InlineData(0, 0)] // Overlap exactly
            [InlineData(0, 10)] // Output +10 offset over salt
            [InlineData(10, 0)] // salt +10 offset over output
            [InlineData(10, 20)] // Both offset, output +10 over salt
            public void ExtractOverlapsPrkOverSalt(int saltOffset, int outputOffset)
            {
                ReadOnlySpan<byte> ikm = "0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b".HexToByteArray();
                ReadOnlySpan<byte> salt = "000102030405060708090a0b0c".HexToByteArray();
                byte[] expectedPrk = "077709362c2e32df0ddc3f0dc47bba6390b6c73bb50f9c3122ec844ad7c2b3e5".HexToByteArray();

                int length = Math.Max(ikm.Length, expectedPrk.Length) + Math.Max(saltOffset, outputOffset);
                Span<byte> buffer = new byte[length];
                Span<byte> saltBuffer = buffer.Slice(saltOffset, salt.Length);
                Span<byte> outputBuffer = buffer.Slice(outputOffset, expectedPrk.Length);
                salt.CopyTo(saltBuffer);

                HKDF.Extract(HashAlgorithmName.SHA256, ikm, saltBuffer, outputBuffer);
                Assert.Equal(expectedPrk, outputBuffer.ToArray());
            }

            [Theory]
            [InlineData(0, 0)] // Overlap exactly
            [InlineData(0, 10)] // Output +10 offset over info
            [InlineData(10, 0)] // Info +10 offset over output
            [InlineData(10, 20)] // Both offset, output +10 over info
            public void ExpandOverlapsOutputOverInfo(int infoOffset, int outputOffset)
            {
                ReadOnlySpan<byte> info = (
                    "b0b1b2b3b4b5b6b7b8b9babbbcbdbebfc0c1c2c3c4c5c6c7c8c9cacbcccdcecf" +
                    "d0d1d2d3d4d5d6d7d8d9dadbdcdddedfe0e1e2e3e4e5e6e7e8e9eaebecedeeef" +
                    "f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff").HexToByteArray();
                ReadOnlySpan<byte> prk =
                    "06a6b88c5853361a06104c9ceb35b45cef760014904671014a193f40c15fc244".HexToByteArray();
                byte[] expectedOkm = (
                    "b11e398dc80327a1c8e7f78c596a49344f012eda2d4efad8a050cc4c19afa97c" +
                    "59045a99cac7827271cb41c65e590e09da3275600c2f09b8367793a9aca3db71" +
                    "cc30c58179ec3e87c14c01d5c1f3434f1d87").HexToByteArray();

                int length = Math.Max(info.Length, expectedOkm.Length) + Math.Max(infoOffset, outputOffset);
                Span<byte> buffer = new byte[length];
                Span<byte> infoBuffer = buffer.Slice(infoOffset, info.Length);
                Span<byte> outputBuffer = buffer.Slice(outputOffset, expectedOkm.Length);
                info.CopyTo(infoBuffer);

                HKDF.Expand(HashAlgorithmName.SHA256, prk, output: outputBuffer, info: infoBuffer);
                Assert.Equal(expectedOkm, outputBuffer.ToArray());
            }

            [Theory]
            [InlineData(0, 0)] // Overlap exactly
            [InlineData(0, 10)] // Output +10 offset over info
            [InlineData(10, 0)] // Info +10 offset over output
            [InlineData(10, 20)] // Both offset, output +10 over info
            public void ExpandOverlapsOutputOverInfoShortOkm(int infoOffset, int outputOffset)
            {
                ReadOnlySpan<byte> info = (
                    "b0b1b2b3b4b5b6b7b8b9babbbcbdbebfc0c1c2c3c4c5c6c7c8c9cacbcccdcecf" +
                    "d0d1d2d3d4d5d6d7d8d9dadbdcdddedfe0e1e2e3e4e5e6e7e8e9eaebecedeeef" +
                    "f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff").HexToByteArray();
                ReadOnlySpan<byte> prk =
                    "06a6b88c5853361a06104c9ceb35b45cef760014904671014a193f40c15fc244".HexToByteArray();
                byte[] expectedOkm =
                    "b11e398dc80327a1c8e7f78c596a49344f012eda2d4efad8a050cc4c19afa97c".HexToByteArray();

                int length = Math.Max(info.Length, expectedOkm.Length) + Math.Max(infoOffset, outputOffset);
                Span<byte> buffer = new byte[length];
                Span<byte> infoBuffer = buffer.Slice(infoOffset, info.Length);
                Span<byte> outputBuffer = buffer.Slice(outputOffset, expectedOkm.Length);
                info.CopyTo(infoBuffer);

                HKDF.Expand(HashAlgorithmName.SHA256, prk, output: outputBuffer, info: infoBuffer);
                Assert.Equal(expectedOkm, outputBuffer.ToArray());
            }

            [Theory]
            [InlineData(0, 0)] // Overlap exactly
            [InlineData(0, 10)] // Output +10 offset over prk
            [InlineData(10, 0)] // Prk +10 offset over output
            [InlineData(10, 20)] // Both offset, output +10 over prk
            public void ExpandOverlapsOutputOverPrk(int prkOffset, int outputOffset)
            {
                ReadOnlySpan<byte> info = (
                    "b0b1b2b3b4b5b6b7b8b9babbbcbdbebfc0c1c2c3c4c5c6c7c8c9cacbcccdcecf" +
                    "d0d1d2d3d4d5d6d7d8d9dadbdcdddedfe0e1e2e3e4e5e6e7e8e9eaebecedeeef" +
                    "f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff").HexToByteArray();
                ReadOnlySpan<byte> prk =
                    "06a6b88c5853361a06104c9ceb35b45cef760014904671014a193f40c15fc244".HexToByteArray();
                byte[] expectedOkm = (
                    "b11e398dc80327a1c8e7f78c596a49344f012eda2d4efad8a050cc4c19afa97c" +
                    "59045a99cac7827271cb41c65e590e09da3275600c2f09b8367793a9aca3db71" +
                    "cc30c58179ec3e87c14c01d5c1f3434f1d87").HexToByteArray();

                int length = Math.Max(prk.Length, expectedOkm.Length) + Math.Max(prkOffset, outputOffset);
                Span<byte> buffer = new byte[length];
                Span<byte> prkBuffer = buffer.Slice(prkOffset, prk.Length);
                Span<byte> outputBuffer = buffer.Slice(outputOffset, expectedOkm.Length);
                prk.CopyTo(prkBuffer);

                HKDF.Expand(HashAlgorithmName.SHA256, prkBuffer, output: outputBuffer, info: info);
                Assert.Equal(expectedOkm, outputBuffer.ToArray());
            }
        }
    }
}
