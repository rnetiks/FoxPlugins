namespace Compositor.KK
{
    public enum SocketType
    {
        /// <summary>
        /// Most common type of socket, used for full RGBA color data
        /// </summary>
        RGBA,
        /// <summary>
        /// Used as the main component for a single value per pixel
        /// </summary>
        Alpha,
        /// <summary>
        /// Single Value, XYZ data
        /// </summary>
        Vector,
        UV,
        Text
    }
}