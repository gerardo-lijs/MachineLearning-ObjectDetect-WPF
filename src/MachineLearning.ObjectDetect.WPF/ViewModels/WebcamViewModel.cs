using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Disposables;

using System.Drawing;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

using OpenCvSharp.Extensions;
using OnnxObjectDetection;

namespace MachineLearning.ObjectDetect.WPF.ViewModels
{
    public class WebcamViewModel : ReactiveObject, IActivatableViewModel, IRoutableViewModel
    {
        public string UrlPathSegment => "WebcamView";
        public IScreen HostScreen { get; }
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        private readonly CompositeDisposable _cleanUp = new CompositeDisposable();
        private readonly MainWindowViewModel _mainViewModel;

        public Services.CameraOpenCv CameraOpenCv { get; }

        public List<Services.CameraOpenCv.CameraDevice> CameraDevices { get; }
        [Reactive] public Services.CameraOpenCv.CameraDevice? CameraDeviceSelected { get; set; }

        public List<BoundingBox> FilteredBoundingBoxes { get; private set; } = new List<BoundingBox>();

        // Commands
        public ReactiveCommand<Unit, Unit> NavigateBack { get; }
        public ReactiveCommand<Unit, Unit> GrabContinuous_Start { get; }
        public ReactiveCommand<Unit, Unit> GrabContinuous_Stop { get; }

        // Interactions
        public readonly Interaction<Unit, Unit> DrawOverlays = new Interaction<Unit, Unit>();

        [Reactive] public bool DetectObjects { get; set; }

        public WebcamViewModel(IScreen? screen = null)
        {
            HostScreen = screen ?? Locator.Current.GetService<IScreen>();
            _mainViewModel = HostScreen as MainWindowViewModel ?? throw new Exception("IScreen must be of type MainWindowViewModel");

            // Create command
            NavigateBack = ReactiveCommand.CreateFromTask(NavigateBackImpl);
            GrabContinuous_Start = ReactiveCommand.CreateFromTask(GrabContinuous_StartImpl);
            GrabContinuous_Stop = ReactiveCommand.CreateFromTask(GrabContinuous_StopImpl);

            // Enumerate cameras
            CameraDevices = Services.CameraOpenCv.EnumerateAllConnectedCameras().ToList();
            CameraDeviceSelected = CameraDevices.FirstOrDefault();

            // For this sample we init the camera here. We could also have a Singleton camera and get it directly with DI
            CameraOpenCv = new Services.CameraOpenCv();
            //CameraOpenCv.GrabContinuousStarted += CameraOpenCv_GrabContinuousStarted;
            //CameraOpenCv.GrabContinuousStopped += CameraOpenCv_GrabContinuousStopped;
            CameraOpenCv.ImageGrabbed.Subscribe(async imageGrabbedData =>
                {
                    // Check
                    if (!DetectObjects) return;

                    // Detect
                    var imageInputData = new ImageInputData { Image = imageGrabbedData.image.ToBitmap() };
                    FilteredBoundingBoxes = _mainViewModel.DetectObjects(imageInputData);
                    await DrawOverlays.Handle(Unit.Default);
                })
                .DisposeWith(_cleanUp);

            this.WhenActivated(disposables =>
            {
                Disposable
                    .Create(() => HandleDeactivation())
                    .DisposeWith(disposables);
            });
        }

        private void HandleDeactivation()
        {
            _cleanUp.Dispose();
            CameraOpenCv?.Dispose();
        }

        private async Task NavigateBackImpl()
        {
            await GrabContinuous_Stop.Execute();
            await _mainViewModel.Router.NavigateBack.Execute();
        }

        private async Task GrabContinuous_StartImpl()
        {
            // Check
            if (CameraDeviceSelected is null) throw new Exception("Camera device not selected.");

            // Grab
            await CameraOpenCv.GrabContinuous_Start(CameraDeviceSelected.CameraIndex);
        }

        private async Task GrabContinuous_StopImpl()
        {
            // Cancel and dispose
            await CameraOpenCv.GrabContinuous_Stop();
        }
    }
}
