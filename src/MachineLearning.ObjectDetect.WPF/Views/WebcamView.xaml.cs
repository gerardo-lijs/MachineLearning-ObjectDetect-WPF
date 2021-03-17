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
using OpenCvSharp.WpfExtensions;

using MachineLearning.ObjectDetect.WPF.ViewModels;

namespace MachineLearning.ObjectDetect.WPF.Views
{
    public class WebcamViewBase : ReactiveUserControl<WebcamViewModel> { }
    public partial class WebcamView : WebcamViewBase
    {
        public WebcamView()
        {
            InitializeComponent();
            this.WhenActivated(disposableRegistration =>
            {
                this.WhenAnyValue(viewModel => viewModel.ViewModel).BindTo(this, view => view.DataContext).DisposeWith(disposableRegistration);

                // Commands
                this.BindCommand(ViewModel, viewModel => viewModel.NavigateBack, view => view.NavigateBackButton).DisposeWith(disposableRegistration);
                this.BindCommand(ViewModel, viewModel => viewModel.GrabContinuous_Start, view => view.WebcamStartButton).DisposeWith(disposableRegistration);
                this.BindCommand(ViewModel, viewModel => viewModel.GrabContinuous_Stop, view => view.WebcamStopButton).DisposeWith(disposableRegistration);

                // Device Combobox
                CameraDeviceComboBox.DisplayMemberPath = nameof(Services.CameraOpenCv.CameraDevice.Name);
                this.OneWayBind(ViewModel, viewModel => viewModel.CameraDevices, view => view.CameraDeviceComboBox.ItemsSource).DisposeWith(disposableRegistration);
                this.Bind(ViewModel, viewModel => viewModel.CameraDeviceSelected, view => view.CameraDeviceComboBox.SelectedValue).DisposeWith(disposableRegistration);

                // Start/Stop buttons
                this.OneWayBind(ViewModel, viewModel => viewModel.CameraOpenCv.IsGrabbing, view => view.WebcamStartButton.IsEnabled, x => !x).DisposeWith(disposableRegistration);
                this.OneWayBind(ViewModel, viewModel => viewModel.CameraOpenCv.IsGrabbing, view => view.WebcamStopButton.IsEnabled).DisposeWith(disposableRegistration);

                // Checkbox
                this.Bind(ViewModel, viewModel => viewModel.CameraOpenCv.FlipImageY, view => view.FlipImageYToggleSwitch.IsOn).DisposeWith(disposableRegistration);
                this.Bind(ViewModel, viewModel => viewModel.CameraOpenCv.FlipImageX, view => view.FlipImageXToggleSwitch.IsOn).DisposeWith(disposableRegistration);
                this.Bind(ViewModel, viewModel => viewModel.DetectObjects, view => view.DetectObjectsToggleSwitch.IsOn).DisposeWith(disposableRegistration);

                ViewModel.CameraOpenCv.ImageGrabbed.Subscribe(async imageGrabbedData =>
                {
                    // Update frame in UI thread
                    try
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            CurrentFPSTextBlock.Text = imageGrabbedData.CurrentFPS.ToString("N1");
                            WebcamImage.Source = imageGrabbedData.image.ToBitmapSource();
                        });
                    }
                    catch (TaskCanceledException)
                    {
                        // App shutting down
                    }
                }).DisposeWith(disposableRegistration);

                // Interactions
                ViewModel.DrawOverlays.RegisterHandler(async interaction =>
                {
                    // Update frame in UI thread
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        DrawOverlays();
                    });
                    interaction.SetOutput(Unit.Default);
                }).DisposeWith(disposableRegistration);

                // Clean up logic to execute when the view model gets deactivated.
                Disposable
                    .Create(() => HandleDeactivation())
                    .DisposeWith(disposableRegistration);
            });
        }

        private void HandleDeactivation()
        {
            ViewModel.CameraOpenCv.Dispose();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e) => DrawOverlays();
        private void DetectImage_SizeChanged(object sender, SizeChangedEventArgs e) => DrawOverlays();

        private void DrawOverlays()
        {
            if (ViewModel is null) return;

            var originalHeight = WebcamImage.ActualHeight;
            var originalWidth = WebcamImage.ActualWidth;

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
