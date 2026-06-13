using System.Runtime.InteropServices;

#pragma warning disable CS1591

namespace MiniVNCClient.Data
{
    /// <summary>
    /// Represents a color with 8-bit values (0-255)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Color8
    {
        internal static Color8 Read(BinaryStream stream) => new()
        {
            Red = stream.ReadByte(),
            Green = stream.ReadByte(),
            Blue = stream.ReadByte()
        };

        internal readonly void Write(BinaryStream stream)
        {
            stream.Write(Red);
            stream.Write(Green);
            stream.Write(Blue);
        }

        public byte Red;
        public byte Green;
        public byte Blue;

        public override readonly string ToString() => $"Red = 0x{Red:x2}, Green = 0x{Green:x2}, Blue = 0x{Blue:x2}";
    }

    /// <summary>
    /// Represents a color with 16-bit values (0-65536)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Color16
    {
        internal static Color16 Read(BinaryStream stream) => new()
        {
            Red = stream.ReadUInt16(),
            Green = stream.ReadUInt16(),
            Blue = stream.ReadUInt16()
        };

        internal readonly void Write(BinaryStream stream)
        {
            stream.Write(Red);
            stream.Write(Green);
            stream.Write(Blue);
        }

        public ushort Red;
        public ushort Green;
        public ushort Blue;

        public override readonly string ToString() => $"Red = 0x{Red:x4}, Green = 0x{Green:x4}, Blue = 0x{Blue:x4}";
    }
}

#pragma warning restore CS1591
