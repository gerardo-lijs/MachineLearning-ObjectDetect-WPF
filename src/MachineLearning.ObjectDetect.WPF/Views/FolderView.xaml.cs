using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ReactiveUI;

using MachineLearning.ObjectDetect.WPF.ViewModels;

namespace MachineLearning.ObjectDetect.WPF.Views
{
    public class FolderViewBase : ReactiveUserControl<FolderViewModel> { }
    public partial class FolderView : FolderViewBase
    {
        public FolderView()
        {
            InitializeComponent();
            this.WhenActivated(async disposableRegistration =>
            {
                this.WhenAnyValue(viewModel => viewModel.ViewModel).BindTo(this, view => view.DataContext).DisposeWith(disposableRegistration);

                // Commands
                this.BindCommand(ViewModel, viewModel => viewModel.PrevImage, view => view.PrevImageButton).DisposeWith(disposableRegistration);
                this.BindCommand(ViewModel, viewModel => viewModel.NextImage, view => view.NextImageButton).DisposeWith(disposableRegistration);
                this.BindCommand(ViewModel, viewModel => viewModel.SelectImageFolder, view => view.SelectImageFolderButton).DisposeWith(disposableRegistration);

                // Image
                this.OneWayBind(ViewModel, viewModel => viewModel.ImageFolderPath, view => view.ImageFolderPathTextBox.Text).DisposeWith(disposableRegistration);
                this.OneWayBind(ViewModel, viewModel => viewModel.DetectImageSource, view => view.DetectImage.Source).DisposeWith(disposableRegistration);
                this.OneWayBind(ViewModel, viewModel => viewModel.DetectMilliseconds, view => view.StatusTextBlock.Text, x => $"Object detection took {x} milliseconds").DisposeWith(disposableRegistration);

                // Visibility
                this.OneWayBind(ViewModel, viewModel => viewModel.DetectMilliseconds, view => view.StatusTextBlock.Visibility, x => x > 0 ? Visibility.Visible : Visibility.Hidden).DisposeWith(disposableRegistration);
                this.OneWayBind(ViewModel, viewModel => viewModel.ImageCurrentIndex, view => view.PrevImageButton.Visibility, x => x <= 1 ? Visibility.Hidden : Visibility.Visible).DisposeWith(disposableRegistration);
                this.OneWayBind(ViewModel, viewModel => viewModel.ImageCurrentIndex, view => view.NextImageButton.Visibility, x => x == ViewModel.ImageList.Count ? Visibility.Hidden : Visibility.Visible).DisposeWith(disposableRegistration);

                // Interactions
                ViewModel.DrawOverlays.RegisterHandler(interaction =>
                {
                    DrawOverlays();
                    interaction.SetOutput(Unit.Default);
                }).DisposeWith(disposableRegistration);

                // Move to first image
                if (ViewModel.ImageList.Count > 0)
                {
                    await ViewModel.NextImage.Execute();
                }
            });
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e) => DrawOverlays();
        private void DetectImage_SizeChanged(object sender, SizeChangedEventArgs e) => DrawOverlays();

        private void DrawOverlays()
        {
            if (ViewModel is null) return;

            var originalHeight = DetectImage.ActualHeight;
            var originalWidth = DetectImage.ActualWidth;

            WebCamCanvas.Children.Clear();

            foreach (var box in ViewModel.FilteredBoundingBoxes)
            {
                // Process output boxes
                double x = Math.Max(box.Dimensions.X, 0);
                double y = Math.Max(box.Dimensions.Y, 0);
                double width = Math.Min(originalWidth - x, box.Dimensions.Width);
                double height = Math.Min(originalHeight - y, box.Dimensions.Height);

                // Fit to current image size
                x = originalWidth * x / OnnxObjectDetection.ImageSettings.imageWidth;
                y = originalHeight * y / OnnxObjectDetection.ImageSettings.imageHeight;
                width = originalWidth * width / OnnxObjectDetection.ImageSettings.imageWidth;
                height = originalHeight * height / OnnxObjectDetection.ImageSettings.imageHeight;

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
    }
}
