using System.Buffers.Binary;
using System.Security.Cryptography;

namespace MiniVNCClient.Security
{
    internal class VNCAuthHandler : IAuthHandler
    {
        public SecurityResult Handle(Client client, BinaryStream stream)
        {
            if (client.Password == null)
            {
                throw new Exception("Empty password");
            }

            using var des = DES.Create();
            des.Key = [0xe8, 0x4a, 0xd6, 0x60, 0xc4, 0x72, 0x1a, 0xe0];

            var password = des.DecryptCbc(client.Password, new byte[8], PaddingMode.None);

            des.Key = [.. password
                .Select(item =>
                    (byte)
                    (
                        ((item >> 7) & 0x01)
                        | (((item >> 6) & 0x01) << 1)
                        | (((item >> 5) & 0x01) << 2)
                        | (((item >> 4) & 0x01) << 3)
                        | (((item >> 3) & 0x01) << 4)
                        | (((item >> 2) & 0x01) << 5)
                        | (((item >> 1) & 0x01) << 6)
                        | ((item & 0x01) << 7)
                    )
                )
            ];

            stream.Write((byte)SecurityType.VNCAuthentication);
            stream.Write(des.EncryptEcb(stream.ReadBytes(16), PaddingMode.None));

            return (SecurityResult)stream.ReadUInt32();
        }
    }
}
