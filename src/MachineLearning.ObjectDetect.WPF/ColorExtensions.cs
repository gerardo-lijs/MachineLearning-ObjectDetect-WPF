namespace MachineLearning.ObjectDetect.WPF
{
    internal static class ColorExtensions
    {
        internal static System.Windows.Media.Color ToMediaColor(this System.Drawing.Color drawingColor) => System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
    }
}
