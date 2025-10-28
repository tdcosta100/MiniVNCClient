using System.Runtime.InteropServices;

namespace MiniVNCClient.Data.RectangleEncodings
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CopyRectRectangleData : IRectangleData
    {
        public ushort SourceX;
        public ushort SourceY;
    }
}
