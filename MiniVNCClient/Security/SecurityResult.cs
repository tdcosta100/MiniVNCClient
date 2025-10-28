namespace MiniVNCClient.Security
{
    internal enum SecurityResult : uint
    {
        OK = 0,
        Failed = 1,
        FailedTooManyAttempts = 2
    }
}
