using HelperUtilities.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;

namespace HelperUtilities
{
    public class Util
    {
        public static List<FileInfo> ReadAllfilesOnSelectedPath(string path)
        {
            List<FileInfo> fileInfos = new List<FileInfo>();
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();

            if (directoryInfos.Any())
                foreach (DirectoryInfo directory in directoryInfos)
                    fileInfos.AddRange(ReadAllfilesOnSelectedPath(directory.FullName));
            else
                fileInfos.AddRange(directoryInfo.GetFiles());

            return fileInfos;
        }

        public static TTarget Mapper<TSource, TTarget>(TSource source) where TSource : class where TTarget : class
        {
            Func<TSource, TTarget> mapper = CreateMapper<TSource, TTarget>();
            return mapper(source);
        }

        public static Func<TSource, TTarget> CreateMapper<TSource, TTarget>() where TTarget : class where TSource : class
        {
            var sourceProperties = typeof(TSource)
                .GetProperties()
                .Where(x => x.CanRead);
            var targetProperties = typeof(TTarget)
                .GetProperties()
                .Where(x => x.CanWrite)
                .ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

            var source = Expression.Parameter(typeof(TSource), "source");
            var target = Expression.Variable(typeof(TTarget));
            var allocate = Expression.New(typeof(TTarget));
            var assignTarget = Expression.Assign(target, allocate);

            var statements = new List<Expression>();
            statements.Add(assignTarget);

            foreach (var sourceProperty in sourceProperties)
            {
                PropertyInfo targetProperty;
                if (targetProperties.TryGetValue(sourceProperty.Name, out targetProperty))
                {
                    var assignProperty = Expression.Assign(
                        Expression.Property(target, targetProperty),
                        Expression.Property(source, sourceProperty));
                    statements.Add(assignProperty);
                }
            }

            statements.Add(target);

            var body = Expression.Block(new[] { target }, statements);

            return Expression.Lambda<Func<TSource, TTarget>>(body, source).Compile();
        }

        public enum PemStringType
        {
            Certificate,
            RsaPrivateKey
        }
        /// <summary>
        /// This helper function parses an integer size from the reader using the ASN.1 format
        /// </summary>
        /// <param name="rd"></param>
        /// <returns></returns>
        public static int DecodeIntegerSize(BinaryReader rd)
        {
            byte byteValue;
            int count;

            byteValue = rd.ReadByte();
            if (byteValue != 0x02)        // indicates an ASN.1 integer value follows
                return 0;

            byteValue = rd.ReadByte();
            if (byteValue == 0x81)
            {
                count = rd.ReadByte();    // data size is the following byte
            }
            else if (byteValue == 0x82)
            {
                byte hi = rd.ReadByte();  // data size in next 2 bytes
                byte lo = rd.ReadByte();
                count = BitConverter.ToUInt16(new[] { lo, hi }, 0);
            }
            else
            {
                count = byteValue;        // we already have the data size
            }

            //remove high order zeros in data
            while (rd.ReadByte() == 0x00)
            {
                count -= 1;
            }
            rd.BaseStream.Seek(-1, SeekOrigin.Current);

            return count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pemString"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static byte[] GetBytesFromPEM(string pemString, PemStringType type)
        {
            string header; string footer;

            switch (type)
            {
                case PemStringType.Certificate:
                    header = "-----BEGIN CERTIFICATE-----";
                    footer = "-----END CERTIFICATE-----";
                    break;
                case PemStringType.RsaPrivateKey:
                    header = "-----BEGIN RSA PRIVATE KEY-----";
                    footer = "-----END RSA PRIVATE KEY-----";
                    break;
                default:
                    return null;
            }

            int start = pemString.IndexOf(header) + header.Length;
            int end = pemString.IndexOf(footer, start) - start;
            return Convert.FromBase64String(pemString.Substring(start, end));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputBytes"></param>
        /// <param name="alignSize"></param>
        /// <returns></returns>
        public static byte[] AlignBytes(byte[] inputBytes, int alignSize)
        {
            int inputBytesSize = inputBytes.Length;

            if (alignSize != -1 && inputBytesSize < alignSize)
            {
                byte[] buf = new byte[alignSize];
                for (int i = 0; i < inputBytesSize; ++i)
                {
                    buf[i + (alignSize - inputBytesSize)] = inputBytes[i];
                }
                return buf;
            }
            else
            {
                return inputBytes;      // Already aligned, or doesn't need alignment
            }
        }

        /// <summary>
        /// This helper function parses an RSA private key using the ASN.1 format
        /// </summary>
        /// <param name="privateKeyBytes">Byte array containing PEM string of private key.</param>
        /// <returns>An instance of <see cref="RSACryptoServiceProvider"/> rapresenting the requested private key.
        /// Null if method fails on retriving the key.</returns>
        public static RSACryptoServiceProvider DecodeRsaPrivateKey(byte[] privateKeyBytes)
        {
            MemoryStream ms = new MemoryStream(privateKeyBytes);
            BinaryReader rd = new BinaryReader(ms);

            try
            {
                byte byteValue;
                ushort shortValue;

                shortValue = rd.ReadUInt16();

                switch (shortValue)
                {
                    case 0x8130:
                        // If true, data is little endian since the proper logical seq is 0x30 0x81
                        rd.ReadByte(); //advance 1 byte
                        break;
                    case 0x8230:
                        rd.ReadInt16();  //advance 2 bytes
                        break;
                    default:
                        Debug.Assert(false);     // Improper ASN.1 format
                        return null;
                }

                shortValue = rd.ReadUInt16();
                if (shortValue != 0x0102) // (version number)
                {
                    Debug.Assert(false);     // Improper ASN.1 format, unexpected version number
                    return null;
                }

                byteValue = rd.ReadByte();
                if (byteValue != 0x00)
                {
                    Debug.Assert(false);     // Improper ASN.1 format
                    return null;
                }

                // The data following the version will be the ASN.1 data itself, which in our case
                // are a sequence of integers.

                // In order to solve a problem with instancing RSACryptoServiceProvider
                // via default constructor on .net 4.0 this is a hack
                CspParameters parms = new CspParameters();
                parms.Flags = CspProviderFlags.NoFlags;
                parms.KeyContainerName = Guid.NewGuid().ToString().ToUpperInvariant();
                parms.ProviderType = Environment.OSVersion.Version.Major > 5 || Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1 ? 0x18 : 1;

                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(parms);
                RSAParameters rsAparams = new RSAParameters();

                rsAparams.Modulus = rd.ReadBytes(DecodeIntegerSize(rd));

                // Argh, this is a pain.  From emperical testing it appears to be that RSAParameters doesn't like byte buffers that
                // have their leading zeros removed.  The RFC doesn't address this area that I can see, so it's hard to say that this
                // is a bug, but it sure would be helpful if it allowed that. So, there's some extra code here that knows what the
                // sizes of the various components are supposed to be.  Using these sizes we can ensure the buffer sizes are exactly
                // what the RSAParameters expect.  Thanks, Microsoft.
                RSAParameterTraits traits = new RSAParameterTraits(rsAparams.Modulus.Length * 8);

                rsAparams.Modulus = AlignBytes(rsAparams.Modulus, traits.size_Mod);
                rsAparams.Exponent = AlignBytes(rd.ReadBytes(DecodeIntegerSize(rd)), traits.size_Exp);
                rsAparams.D = AlignBytes(rd.ReadBytes(DecodeIntegerSize(rd)), traits.size_D);
                rsAparams.P = AlignBytes(rd.ReadBytes(DecodeIntegerSize(rd)), traits.size_P);
                rsAparams.Q = AlignBytes(rd.ReadBytes(DecodeIntegerSize(rd)), traits.size_Q);
                rsAparams.DP = AlignBytes(rd.ReadBytes(DecodeIntegerSize(rd)), traits.size_DP);
                rsAparams.DQ = AlignBytes(rd.ReadBytes(DecodeIntegerSize(rd)), traits.size_DQ);
                rsAparams.InverseQ = AlignBytes(rd.ReadBytes(DecodeIntegerSize(rd)), traits.size_InvQ);

                rsa.ImportParameters(rsAparams);
                return rsa;
            }
            catch (Exception)
            {
                Debug.Assert(false);
                return null;
            }
            finally
            {
                rd.Close();
            }
        }
    }
}
