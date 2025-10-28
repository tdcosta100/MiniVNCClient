namespace MiniVNCClient.Data.RectangleEncodings
{
    [Flags]
    internal enum HextileSubencodingMask : byte
    {
        Raw = 1 << 0,
        BackgroundSpecified = 1 << 1,
        ForegroundSpecified = 1 << 2,
        AnySubrects = 1 << 3,
        SubrectsColoured = 1 << 4,
        ZlibRaw = 1 << 5,
        Zlib = 1 << 6
    }
}
