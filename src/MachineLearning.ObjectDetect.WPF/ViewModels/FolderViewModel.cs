using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Windows.Media.Imaging;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

using OnnxObjectDetection;

namespace MachineLearning.ObjectDetect.WPF.ViewModels
{
    public class FolderViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment => "FolderView";
        public IScreen HostScreen { get; }
        private readonly MainWindowViewModel _mainViewModel;

        // Commands
        public ReactiveCommand<Unit, Unit> PrevImage { get; }
        public ReactiveCommand<Unit, Unit> NextImage { get; }
        public ReactiveCommand<Unit, Unit> SelectImageFolder { get; }

        [Reactive] public string ImageFolderPath { get; private set; } = string.Empty;
        [Reactive] public List<string> ImageList { get; private set; } = new List<string>();
        [Reactive] public int ImageCurrentIndex { get; private set; }

        [Reactive] public long DetectMilliseconds { get; private set; }
        [Reactive] public BitmapSource? DetectImageSource { get; private set; }
        public List<BoundingBox> FilteredBoundingBoxes { get; private set; } = new List<BoundingBox>();

        // Interactions
        public readonly Interaction<Unit, Unit> DrawOverlays = new Interaction<Unit, Unit>();

        public FolderViewModel(IScreen? screen = null)
        {
            HostScreen = screen ?? Locator.Current.GetService<IScreen>();
            _mainViewModel = HostScreen as MainWindowViewModel ?? throw new Exception("IScreen must be of type MainWindowViewModel");

            // Create command
            PrevImage = ReactiveCommand.CreateFromTask(PrevImageImpl);
            NextImage = ReactiveCommand.CreateFromTask(NextImageImpl);
            SelectImageFolder = ReactiveCommand.CreateFromTask(SelectImageFolderImpl);

            // Observables
            this.WhenAnyValue(x => x.ImageFolderPath)
                .Skip(1)
                .Subscribe(folder =>
                {
                    if (string.IsNullOrWhiteSpace(folder)) return;
                    ImageList = System.IO.Directory.GetFiles(folder).Where(x => x.ToLowerInvariant().EndsWith(".png") || x.ToLowerInvariant().EndsWith(".jpg") || x.ToLowerInvariant().EndsWith(".jpeg") || x.ToLowerInvariant().EndsWith(".bmp")).ToList();
                });

            // Load image list
            ImageFolderPath = System.IO.Path.Combine(Environment.CurrentDirectory, "TestImages");
        }

        private async Task SelectImageFolderImpl()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                SelectedPath = ImageFolderPath
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ImageFolderPath = dialog.SelectedPath;
                ImageCurrentIndex = 0;
                if (ImageList.Count > 0)
                {
                    await NextImage.Execute();
                }
            }
        }

        private async Task PrevImageImpl()
        {
            if (ImageCurrentIndex <= 1) return;
            ImageCurrentIndex--;
            await LoadAndDetectImage(ImageList[ImageCurrentIndex - 1]);
        }

        private async Task NextImageImpl()
        {
            if (ImageList.Count == ImageCurrentIndex) return;
            ImageCurrentIndex++;
            await LoadAndDetectImage(ImageList[ImageCurrentIndex - 1]);
        }

        private async Task DetectImage(Bitmap bitmap)
        {
            var imageInputData = new ImageInputData { Image = bitmap };

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var labels = _mainViewModel.CustomVisionPredictionEngine.Predict(imageInputData).PredictedLabels;
            var boundingBoxes = _mainViewModel.OutputParser.ParseOutputs(labels);
            FilteredBoundingBoxes = _mainViewModel.OutputParser.FilterBoundingBoxes(boundingBoxes, 5, 0.5f);

            // Time spent for detection by ML.NET
            DetectMilliseconds = sw.ElapsedMilliseconds;

            await DrawOverlays.Handle(Unit.Default);
        }

        private async Task LoadAndDetectImage(string filename)
        {
            var bitmapImage = new Bitmap(filename);
            DetectImageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bitmapImage.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            await DetectImage(bitmapImage);
        }
    }
}
