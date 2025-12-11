using System;
using System.Security.Cryptography;
using System.Text;

namespace SuiIdLengthDemo
{
    internal static class Program
    {
        private sealed class Organisation
        {
            public string Name { get; }
            public byte[] Key { get; }

            public Organisation(string name, byte[] key)
            {
                Name = name;
                Key = key;
            }
        }

        static void Main()
        {
            const string sui = "AB9434765X";
            const byte version = 1;
            const int ttlEncoded = 20300101;

            byte[] payloadSuiOnly = BuildPayloadSuiOnly(sui);
            byte[] payloadWithTtl = BuildPayloadWithTtl(sui, version, ttlEncoded);

            Console.WriteLine("Here is the SUI:      " + sui);
            Console.WriteLine("Here is the version:  " + version);
            Console.WriteLine("Here is the TTL:      " + ttlEncoded);
            Console.WriteLine();

            Console.WriteLine("Here is the payload (SUI only) bytes:");
            Console.WriteLine(BytesToHex(payloadSuiOnly));
            Console.WriteLine();

            Console.WriteLine("Here is the payload (SUI + version + TTL) bytes:");
            Console.WriteLine(BytesToHex(payloadWithTtl));
            Console.WriteLine();

            // Two different organisations with different keys
            var orgAKey = BuildKey(0x01);
            var orgBKey = BuildKey(0x21);

            var orgA = new Organisation("Org A", orgAKey);
            var orgB = new Organisation("Org B", orgBKey);

            Console.WriteLine("Organisation keys (AES-256, 32 bytes each):");
            Console.WriteLine($"  {orgA.Name} key: {BytesToHex(orgA.Key)}");
            Console.WriteLine($"  {orgB.Name} key: {BytesToHex(orgB.Key)}");
            Console.WriteLine();

            // Encrypt for each organisation
            var orgA_cipherSuiOnly = EncryptAes256Block(payloadSuiOnly, orgA.Key);
            var orgA_cipherWithTtl = EncryptAes256Block(payloadWithTtl, orgA.Key);

            var orgB_cipherSuiOnly = EncryptAes256Block(payloadSuiOnly, orgB.Key);
            var orgB_cipherWithTtl = EncryptAes256Block(payloadWithTtl, orgB.Key);

            var orgA_idSuiOnly = EncodeId(orgA_cipherSuiOnly);
            var orgA_idWithTtl = EncodeId(orgA_cipherWithTtl);

            var orgB_idSuiOnly = EncodeId(orgB_cipherSuiOnly);
            var orgB_idWithTtl = EncodeId(orgB_cipherWithTtl);

            Console.WriteLine("Resulting identifiers (URL-safe Base64, no padding):");
            Console.WriteLine("Org   | idSuiOnly                 | idWithTtl");
            Console.WriteLine("------+---------------------------+---------------------------");
            Console.WriteLine($"{orgA.Name,-5}| {orgA_idSuiOnly,-25} | {orgA_idWithTtl,-25}");
            Console.WriteLine($"{orgB.Name,-5}| {orgB_idSuiOnly,-25} | {orgB_idWithTtl,-25}");
            Console.WriteLine();

            Console.WriteLine("Lengths of identifiers:");
            Console.WriteLine($"{orgA.Name} idSuiOnly length:  {orgA_idSuiOnly.Length}");
            Console.WriteLine($"{orgA.Name} idWithTtl length: {orgA_idWithTtl.Length}");
            Console.WriteLine($"{orgB.Name} idSuiOnly length:  {orgB_idSuiOnly.Length}");
            Console.WriteLine($"{orgB.Name} idWithTtl length: {orgB_idWithTtl.Length}");
            Console.WriteLine();

            // Decrypt and prove we get back original content
            Console.WriteLine("Decrypting identifiers to prove round-trip for each organisation:");
            Console.WriteLine();

            DemonstrateDecryption(orgA, orgA_idSuiOnly, orgA_idWithTtl);
            Console.WriteLine();
            DemonstrateDecryption(orgB, orgB_idSuiOnly, orgB_idWithTtl);

            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        }

        private static void DemonstrateDecryption(Organisation org, string idSuiOnly, string idWithTtl)
        {
            Console.WriteLine($"=== {org.Name} ===");

            byte[] cipherSuiOnly = DecodeId(idSuiOnly);
            byte[] cipherWithTtl = DecodeId(idWithTtl);

            byte[] recoveredPayloadSuiOnly = DecryptAes256Block(cipherSuiOnly, org.Key);
            byte[] recoveredPayloadWithTtl = DecryptAes256Block(cipherWithTtl, org.Key);

            string recoveredSuiFromSuiOnly = ExtractSuiFromPayloadSuiOnly(recoveredPayloadSuiOnly);

            ExtractFromPayloadWithTtl(
                recoveredPayloadWithTtl,
                out string recoveredSuiFromWithTtl,
                out byte recoveredVersion,
                out int recoveredTtl
            );

            Console.WriteLine("Recovered from idSuiOnly:");
            Console.WriteLine("  SUI: " + recoveredSuiFromSuiOnly);
            Console.WriteLine();

            Console.WriteLine("Recovered from idWithTtl:");
            Console.WriteLine("  SUI:     " + recoveredSuiFromWithTtl);
            Console.WriteLine("  Version: " + recoveredVersion);
            Console.WriteLine("  TTL:     " + recoveredTtl);
        }

        private static byte[] BuildKey(byte startValue)
        {
            byte[] key = new byte[32];

            byte value = startValue;
            for (int i = 0; i < key.Length; i++)
            {
                key[i] = value;
                value++;
            }

            return key;
        }

        private static byte[] BuildPayloadSuiOnly(string sui)
        {
            byte[] block = new byte[16];

            byte[] suiBytes = Encoding.UTF8.GetBytes(sui);

            if (suiBytes.Length > 16)
            {
                Array.Copy(suiBytes, 0, block, 0, 16);
            }
            else
            {
                Array.Copy(suiBytes, 0, block, 0, suiBytes.Length);
            }

            return block;
        }

        private static byte[] BuildPayloadWithTtl(string sui, byte version, int ttlEncoded)
        {
            byte[] block = new byte[16];

            byte[] suiBytes = Encoding.UTF8.GetBytes(sui);

            int maxSuiBytes = 10;

            if (suiBytes.Length > maxSuiBytes)
            {
                Array.Copy(suiBytes, 0, block, 0, maxSuiBytes);
            }
            else
            {
                Array.Copy(suiBytes, 0, block, 0, suiBytes.Length);
            }

            block[10] = version;

            byte[] ttlBytes = BitConverter.GetBytes(ttlEncoded);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(ttlBytes);
            }

            Array.Copy(ttlBytes, 0, block, 11, ttlBytes.Length);

            return block;
        }

        private static byte[] EncryptAes256Block(byte[] block, byte[] key)
        {
            if (block.Length != 16)
            {
                throw new ArgumentException("AES block must be exactly 16 bytes.");
            }

            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.Key = key;

            using ICryptoTransform encryptor = aes.CreateEncryptor();

            byte[] result = new byte[16];
            int written = encryptor.TransformBlock(block, 0, block.Length, result, 0);
            if (written != 16)
            {
                throw new InvalidOperationException("Unexpected AES output length.");
            }

            return result;
        }

        private static byte[] DecryptAes256Block(byte[] cipher, byte[] key)
        {
            if (cipher.Length != 16)
            {
                throw new ArgumentException("AES cipher block must be exactly 16 bytes.");
            }

            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.Key = key;

            using ICryptoTransform decryptor = aes.CreateDecryptor();

            byte[] result = new byte[16];
            int written = decryptor.TransformBlock(cipher, 0, cipher.Length, result, 0);
            if (written != 16)
            {
                throw new InvalidOperationException("Unexpected AES output length.");
            }

            return result;
        }

        private static string EncodeId(byte[] data)
        {
            string b64 = Convert.ToBase64String(data);

            b64 = b64.TrimEnd('=')
                     .Replace('+', '-')
                     .Replace('/', '_');

            return b64;
        }

        private static byte[] DecodeId(string id)
        {
            string b64 = id.Replace('-', '+')
                           .Replace('_', '/');

            int mod = b64.Length % 4;
            if (mod == 2)
            {
                b64 += "==";
            }
            else if (mod == 3)
            {
                b64 += "=";
            }

            return Convert.FromBase64String(b64);
        }

        private static string ExtractSuiFromPayloadSuiOnly(byte[] payload)
        {
            int length = 0;
            for (int i = 0; i < payload.Length; i++)
            {
                if (payload[i] == 0)
                {
                    break;
                }
                length++;
            }

            return Encoding.UTF8.GetString(payload, 0, length);
        }

        private static void ExtractFromPayloadWithTtl(byte[] payload, out string sui, out byte version, out int ttlEncoded)
        {
            int maxSuiBytes = 10;

            int suiLength = 0;
            for (int i = 0; i < maxSuiBytes; i++)
            {
                if (payload[i] == 0)
                {
                    break;
                }
                suiLength++;
            }

            sui = Encoding.UTF8.GetString(payload, 0, suiLength);

            version = payload[10];

            byte[] ttlBytes = new byte[4];
            Array.Copy(payload, 11, ttlBytes, 0, 4);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(ttlBytes);
            }

            ttlEncoded = BitConverter.ToInt32(ttlBytes, 0);
        }

        private static string BytesToHex(byte[] data)
        {
            var builder = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
            {
                builder.Append(b.ToString("X2"));
            }
            return builder.ToString();
        }
    }
}
