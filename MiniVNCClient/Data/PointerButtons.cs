namespace MiniVNCClient.Data
{
    /// <summary>
    /// Represents the pressed mouse buttons
    /// </summary>
    [Flags]
    public enum PointerButtons : byte
    {
        /// <summary>
        /// None
        /// </summary>
        None        = 0,

        /// <summary>
        /// Left
        /// </summary>
        Left        = 1 << 0,

        /// <summary>
        /// Middle
        /// </summary>
        Middle      = 1 << 1,

        /// <summary>
        /// Right
        /// </summary>
        Right       = 1 << 2,

        /// <summary>
        /// Scroll Up
        /// </summary>
        ScrollUp    = 1 << 3,

        /// <summary>
        /// Scroll Down
        /// </summary>
        ScrollDown  = 1 << 4,

        /// <summary>
        /// Scroll Left
        /// </summary>
        ScrollLeft  = 1 << 5,

        /// <summary>
        /// Scroll Right
        /// </summary>
        ScrollRight = 1 << 6,

        /// <summary>
        /// Back
        /// </summary>
        Back        = 1 << 7
    }
}
