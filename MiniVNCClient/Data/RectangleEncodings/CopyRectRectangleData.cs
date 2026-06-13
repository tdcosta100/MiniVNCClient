using System.Runtime.InteropServices;

namespace MiniVNCClient.Data.RectangleEncodings
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CopyRectRectangleData : IRectangleData
    {
        internal static CopyRectRectangleData Read(BinaryStream stream) => new()
        {
            SourceX = stream.ReadUInt16(),
            SourceY = stream.ReadUInt16()
        };

        internal readonly void Write(BinaryStream stream)
        {
            stream.Write(SourceX);
            stream.Write(SourceY);
        }

        public ushort SourceX;
        public ushort SourceY;
    }
}
