namespace MiniVNCClient.Security
{
    internal interface IAuthContext
    {
        byte[]? Password { get; }
        Version? ServerVersion { get; }
    }
}
