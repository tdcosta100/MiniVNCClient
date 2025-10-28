#pragma warning disable CS1591

namespace MiniVNCClient.Data
{
    /// <summary>
    /// Flags for the ClientFence and ServerFence messages
    /// </summary>
    [Flags]
    public enum FenceFlags : uint
    {
        None        = 0,
        BlockBefore = 1 << 0,
        BlockAfter  = 1 << 1,
        SyncNext    = 1 << 2,
        Request     = (uint)1 << 31
    }
}

#pragma warning restore CS1591