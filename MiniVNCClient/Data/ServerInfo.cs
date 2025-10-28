using System.Runtime.InteropServices;

namespace MiniVNCClient.Data
{
    /// <summary>
    /// Stores the server parameters for the connection
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ServerInfo
    {
        /// <summary>
        /// Width of the remote framebuffer
        /// </summary>
        public ushort FramebufferWidth;

        /// <summary>
        /// Height of the remote framebuffer
        /// </summary>
        public ushort FramebufferHeight;

        /// <summary>
        /// Pixel format of the session
        /// </summary>
        public PixelFormat PixelFormat;

        internal uint NameLength;

        /// <summary>
        /// Name of the remote desktop
        /// </summary>
        public string? Name;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>

        public override readonly string ToString() =>
            $"FramebufferWidth = {FramebufferWidth}, " +
            $"FramebufferHeight = {FramebufferHeight}, " +
            $"PixelFormat = {{{PixelFormat}}}, " +
            $"Name = \"{Name}\"";
    }
}
