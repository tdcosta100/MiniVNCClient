namespace MiniVNCClient.Security
{
    internal interface IAuthHandler
    {
        SecurityResult Handle(IAuthContext client, BinaryStream stream);
    }
}
