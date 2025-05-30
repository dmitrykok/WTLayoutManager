namespace System.Diagnostics
{
    /// <summary>
    /// Provides extension methods for the FileVersionInfo class.
    /// </summary>
    public static class FileVersionInfoExtensions
    {
        /// <summary>
        /// Gets the size of the file associated with the FileVersionInfo object.
        /// </summary>
        /// <param name="fvi">The FileVersionInfo object.</param>
        /// <returns>The size of the file in bytes.</returns>
        public static long FileSize(this FileVersionInfo fvi)
        {
            // Create a new FileInfo object from the file name in the FileVersionInfo object
            // and return its length.
            return new FileInfo(fvi.FileName).Length;
        }
    }
}
