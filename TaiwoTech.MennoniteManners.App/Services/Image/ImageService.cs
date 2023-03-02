using SkiaSharp;
using TaiwoTech.MennoniteManners.App.Extensions;

namespace TaiwoTech.MennoniteManners.App.Services.Image
{
    public static class ImageService
    {
        private const float ImagesPerRowAndColumn = 10;
        private const int SingleImageLength = 110;
        private const float SingleImageMargin = 40;
        
        private const int CombinedImageLength = (int)((SingleImageLength + SingleImageMargin*2) * ImagesPerRowAndColumn); //1900

        /// <summary>
        /// Take a list of images as byte arrays and convert them into one 10x10 image represented as a byte array
        /// </summary>
        /// <param name="imageByteList"></param>
        /// <returns></returns>
        public static byte[] CombineImages(IEnumerable<byte[]> imageByteList)
        {
            var bitmaps = new List<SKBitmap>();
            try
            {
                bitmaps = ConvertImagesToBitmaps(imageByteList);

                using var drawingSurface = SKSurface.Create(new SKImageInfo(CombinedImageLength, CombinedImageLength));
                drawingSurface.Canvas.Clear(SKColors.White);

                //go through each image and draw it on the final image
                var offsetLeft = SingleImageMargin;
                var offsetTop = SingleImageMargin;
                var imageRowCount = 0;

                foreach (var bitmap in bitmaps)
                {
                    drawingSurface.Canvas.DrawBitmap(bitmap, SKRect.Create(offsetLeft, offsetTop, bitmap.Width, bitmap.Height));
                    offsetLeft += SingleImageLength + (SingleImageMargin* 2); //To the right of the old image & To the left of the new image

                    if (++imageRowCount % ImagesPerRowAndColumn == 0)
                    {
                        offsetLeft = SingleImageMargin;
                        offsetTop += SingleImageLength + (SingleImageMargin * 2); //To the bottom of the old image & To the top of the new image
                    }
                }

                var encodedImage = drawingSurface.Snapshot().Encode(SKEncodedImageFormat.Png, 100);
                return encodedImage.AsStream().ToByteArray();
            }
            finally
            {
                //clean up memory
                foreach (var image in bitmaps)
                {
                    image.Dispose();
                }
            }
        }

        private static List<SKBitmap> ConvertImagesToBitmaps(IEnumerable<byte[]> imagesAsByteArrays)
        {
            var images = new List<SKBitmap>();
            foreach (var byteData in imagesAsByteArrays)
            {
                var imageInfo = new SKImageInfo(SingleImageLength, SingleImageLength);
                var bitmap = SKBitmap.Decode(byteData);
                var resizedBitmap = bitmap.Resize(imageInfo, SKFilterQuality.High);

                images.Add(resizedBitmap);
            }

            return images;
        }
    }
}
