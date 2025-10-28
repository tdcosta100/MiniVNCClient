using System.Buffers.Binary;

namespace MiniVNCClient.Security
{
    internal class NoneAuthHandler : IAuthHandler
    {
        public SecurityResult Handle(Client client, BinaryStream stream)
        {
            stream.Write((byte)SecurityType.None);

            if (client.ServerVersion >= Client.Version38)
            {
                return (SecurityResult)stream.ReadUInt32();
            }

            return SecurityResult.OK;
        }
    }
}
