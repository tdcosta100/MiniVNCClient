namespace MiniVNCClient.Security
{
    internal interface IAuthHandler
    {
        SecurityResult Handle(Client client, BinaryStream stream);
    }
}
