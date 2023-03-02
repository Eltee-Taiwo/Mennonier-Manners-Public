namespace TaiwoTech.MennoniteManners.App.Services.DisplaySize;

public class DisplaySizeService : ISingletonService
{
    public double ScreenWidth { get; }
    public double ScreenHeight { get; }

    public DisplaySizeService()
    {
        var screenWidthInPixels = DeviceDisplay.MainDisplayInfo.Width;
        var screenHeightInPixels = DeviceDisplay.MainDisplayInfo.Height;
        var screenDensity = DeviceDisplay.MainDisplayInfo.Density;
        ScreenWidth = screenWidthInPixels / screenDensity;
        ScreenHeight = screenHeightInPixels / screenDensity;
    }

    /// <summary>
    /// Takes a percentage value for width and height then returns a <see cref="Size"/> value based on the pixel density of the screen
    /// </summary>
    /// <param name="widthPercentage">The percentage of the screen width that we want</param>
    /// <param name="heightPercentage">The percentage of the screen height that we want</param>
    /// <returns></returns>
    public Size GetSizeFromPercentages(double widthPercentage, double heightPercentage)
    {
        return new Size(ScreenWidth * widthPercentage / 100, ScreenHeight * heightPercentage / 100);
    }
}
