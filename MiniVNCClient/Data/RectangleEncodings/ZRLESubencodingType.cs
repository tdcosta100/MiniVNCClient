namespace MiniVNCClient.Data.RectangleEncodings
{
    internal enum ZRLESubencodingType : byte
    {
        Raw = 0,
        SolidColor = 1,

        /* 2 to 16 */
        PackedPalette = 2,

        /* 17 to 127 */
        Unused = 17,
        PlainRLE = 128,

        /* 130 to 255 */
        PaletteRLE = 130
    }
}
