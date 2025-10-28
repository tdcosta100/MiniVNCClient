using System.Drawing;
using System.Runtime.InteropServices;

namespace MiniVNCClient.Data.RectangleEncodings
{
    /// <summary>
    /// Describes the position, dimensions and encoding of a VNC rectangle
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RectangleInfo
    {
        /// <summary>
        /// The x-coordinate of the top-left corner of the rectangle.
        /// </summary>
        public ushort X;

        /// <summary>
        /// The y-coordinate of the top-left corner of the rectangle.
        /// </summary>
        public ushort Y;

        /// <summary>
        /// The width of the rectangle.
        /// </summary>
        public ushort Width;

        /// <summary>
        /// The height of the rectangle.
        /// </summary>
        public ushort Height;

        /// <summary>
        /// The <see cref="VNCEncoding"/> of the rectangle
        /// </summary>
        public VNCEncoding Encoding;

        /// <summary>
        /// Converts this rectangle to a <see cref="Rectangle"/>
        /// </summary>
        /// <returns>A <see cref="Rectangle"/> with the VNC rectangle position and dimensions</returns>
        public readonly Rectangle ToSystemDrawingRectangle() => new(X, Y, Width, Height);

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override readonly string ToString() => $"X = {X}, Y = {Y}, Width = {Width}, Height = {Height}, Encoding = {Encoding}";
    }
}
