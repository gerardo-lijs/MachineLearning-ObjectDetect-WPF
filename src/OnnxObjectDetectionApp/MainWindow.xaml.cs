using Microsoft.ML;
using OnnxObjectDetection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace OnnxObjectDetectionApp
{
    public partial class MainWindow : System.Windows.Window
    {
        private OnnxOutputParser outputParser;
        private PredictionEngine<ImageInputData, CustomVisionPrediction> customVisionPredictionEngine;

        private static readonly string modelsDirectory = Path.Combine(Environment.CurrentDirectory, @"ML\OnnxModels");

        private List<string> ImageList;
        private int currentImageIndex;

        public MainWindow()
        {
            InitializeComponent();
            LoadModel();
            ImageList = System.IO.Directory.GetFiles(@"C:\Github\MachineLearning-ObjectDetect-WPF\assets\test", "*.png").ToList();
        }

        private void LoadModel()
        {
            // Check for an Onnx model exported from Custom Vision
            var customVisionExport = Directory.GetFiles(modelsDirectory, "*.zip").FirstOrDefault();

            var customVisionModel = new CustomVisionModel(customVisionExport);
            var modelConfigurator = new OnnxModelConfigurator(customVisionModel);

            outputParser = new OnnxOutputParser(customVisionModel);
            customVisionPredictionEngine = modelConfigurator.GetMlNetPredictionEngine<CustomVisionPrediction>();
        }

        async Task DetectImage(Bitmap bitmap, CancellationToken cancellationToken)
        {
            var frame = new ImageInputData { Image = bitmap };

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var filteredBoxes = DetectObjectsUsingModel(frame);
            sw.Stop();

            if (!cancellationToken.IsCancellationRequested)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusTextBlock.Text = sw.ElapsedMilliseconds.ToString() + " milliseconds";
                    DrawOverlays(filteredBoxes, WebCamImage.ActualHeight, WebCamImage.ActualWidth);
                });
            }
        }

        public List<BoundingBox> DetectObjectsUsingModel(ImageInputData imageInputData)
        {
            var labels = customVisionPredictionEngine.Predict(imageInputData).PredictedLabels;
            var boundingBoxes = outputParser.ParseOutputs(labels);
            var filteredBoxes = outputParser.FilterBoundingBoxes(boundingBoxes, 5, 0.5f);
            return filteredBoxes;
        }

        private void DrawOverlays(List<BoundingBox> filteredBoxes, double originalHeight, double originalWidth)
        {
            WebCamCanvas.Children.Clear();

            foreach (var box in filteredBoxes)
            {
                // process output boxes
                double x = Math.Max(box.Dimensions.X, 0);
                double y = Math.Max(box.Dimensions.Y, 0);
                double width = Math.Min(originalWidth - x, box.Dimensions.Width);
                double height = Math.Min(originalHeight - y, box.Dimensions.Height);

                // fit to current image size
                x = originalWidth * x / ImageSettings.imageWidth;
                y = originalHeight * y / ImageSettings.imageHeight;
                width = originalWidth * width / ImageSettings.imageWidth;
                height = originalHeight * height / ImageSettings.imageHeight;

                var boxColor = box.BoxColor.ToMediaColor();

                var objBox = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Stroke = new SolidColorBrush(boxColor),
                    StrokeThickness = 2.0,
                    Margin = new Thickness(x, y, 0, 0)
                };

                var objDescription = new TextBlock
                {
                    Margin = new Thickness(x + 4, y + 4, 0, 0),
                    Text = box.Description,
                    FontWeight = FontWeights.Bold,
                    Width = 126,
                    Height = 21,
                    TextAlignment = TextAlignment.Center
                };

                var objDescriptionBackground = new Rectangle
                {
                    Width = 134,
                    Height = 29,
                    Fill = new SolidColorBrush(boxColor),
                    Margin = new Thickness(x, y, 0, 0)
                };

                WebCamCanvas.Children.Add(objDescriptionBackground);
                WebCamCanvas.Children.Add(objDescription);
                WebCamCanvas.Children.Add(objBox);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ImageList.Count == currentImageIndex) return;

            var bitmapImage = new Bitmap(ImageList[currentImageIndex]);

            var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bitmapImage.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            WebCamImage.Source = bitmapSource;

            await DetectImage(bitmapImage, CancellationToken.None);

            currentImageIndex++;
        }
    }
}
