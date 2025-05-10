namespace System.Diagnostics
{
    public static class FileVersionInfoExtensions
    {
        public static long FileSize(this FileVersionInfo fvi) =>
            new FileInfo(fvi.FileName).Length;
    }
}
