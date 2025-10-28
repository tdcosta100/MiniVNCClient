using MiniVNCClient.Data.RectangleEncodings;

namespace MiniVNCClient.Data
{
    /// <summary>
    /// Stores the data for the remote cursor
    /// </summary>
    public class CursorData : IRectangleData
    {
        /// <summary>
        /// X-coordinate of the pointer hotspot (tip)
        /// </summary>
        public int HotspotX { get; set; }

        /// <summary>
        /// Y-coordinate of the pointer hotspot (tip)
        /// </summary>
        public int HotspotY { get; set; }

        /// <summary>
        /// Width of the pointer
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of the pointer
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Pixel data, with the last byte of each pixel being used for alpha
        /// </summary>
        public byte[] PixelData { get; set; } = [];
    }
}
