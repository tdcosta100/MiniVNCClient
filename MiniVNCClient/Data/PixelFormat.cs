using System.Runtime.InteropServices;

namespace MiniVNCClient.Data
{
    /// <summary>
    /// Stores the pixel format for the session
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PixelFormat
    {
        internal static PixelFormat Read(BinaryStream stream) => new()
        {
            BitsPerPixel = stream.ReadByte(),
            Depth = stream.ReadByte(),
            BigEndianFlag = stream.ReadByte(),
            TrueColorFlag = stream.ReadByte(),
            RedMax = stream.ReadUInt16(),
            GreenMax = stream.ReadUInt16(),
            BlueMax = stream.ReadUInt16(),
            RedShift = stream.ReadByte(),
            GreenShift = stream.ReadByte(),
            BlueShift = stream.ReadByte(),
            Padding1 = stream.ReadByte(),
            Padding2 = stream.ReadByte(),
            Padding3 = stream.ReadByte()
        };

        internal readonly void Write(BinaryStream stream)
        {
            stream.Write(BitsPerPixel);
            stream.Write(Depth);
            stream.Write(BigEndianFlag);
            stream.Write(TrueColorFlag);
            stream.Write(RedMax);
            stream.Write(GreenMax);
            stream.Write(BlueMax);
            stream.Write(RedShift);
            stream.Write(GreenShift);
            stream.Write(BlueShift);
            stream.Write(Padding1);
            stream.Write(Padding2);
            stream.Write(Padding3);
        }

        /// <summary>
        /// Bits per pixel
        /// </summary>
        public byte BitsPerPixel;

        /// <summary>
        /// Bit depth
        /// </summary>
        public byte Depth;

        /// <summary>
        /// 1 for big-endian, 0 for little-endian
        /// </summary>
        public byte BigEndianFlag;

        /// <summary>
        /// 1 for true color, 0 for indexed palette
        /// </summary>
        public byte TrueColorFlag;

        /// <summary>
        /// Maximum red value
        /// </summary>
        public ushort RedMax;

        /// <summary>
        /// Maximum green value
        /// </summary>
        public ushort GreenMax;

        /// <summary>
        /// Maximum blue value
        /// </summary>
        public ushort BlueMax;

        /// <summary>
        /// Pixel value shift for red
        /// </summary>
        public byte RedShift;

        /// <summary>
        /// Pixel value shift for green
        /// </summary>
        public byte GreenShift;

        /// <summary>
        /// Blue
        /// </summary>
        public byte BlueShift;

        internal byte Padding1;
        internal byte Padding2;
        internal byte Padding3;

        /// <summary>
        /// Bytes per pixel
        /// </summary>
        public readonly int BytesPerPixel => (BitsPerPixel + 7) / 8;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override readonly string ToString() =>
            $"BitsPerPixel = {BitsPerPixel}, " +
            $"Depth = {Depth}, " +
            $"BigEndianFlag = {BigEndianFlag}, " +
            $"TrueColorFlag = {TrueColorFlag}, " +
            $"RedMax = {RedMax}, " +
            $"GreenMax = {GreenMax}, " +
            $"BlueMax = {BlueMax}, " +
            $"RedShift = {RedShift}, " +
            $"GreenShift = {GreenShift}, " +
            $"BlueShift = {BlueShift}";
    }
}
