namespace TaiwoTech.MennoniteManners.App.Extensions
{
    public static class StreamExtensions
    {
        public static byte[] ToByteArray(this Stream sourceStream)
        {
            using var memoryStream = new MemoryStream();
            sourceStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
