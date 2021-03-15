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
    public class SelectViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment => "SelectView";
        public IScreen HostScreen { get; }
        private readonly MainWindowViewModel _mainViewModel;

        public List<string> Models { get; }
        [Reactive] public string? ModelSelected { get; set; }

        // Commands
        public ReactiveCommand<Unit, Unit> FolderViewSelect { get; }
        public ReactiveCommand<Unit, Unit> WebcamViewSelect { get; }

        public SelectViewModel(IScreen? screen = null)
        {
            HostScreen = screen ?? Locator.Current.GetService<IScreen>();
            _mainViewModel = HostScreen as MainWindowViewModel ?? throw new Exception("IScreen must be of type MainWindowViewModel");

            // Enumerate models
            Models = System.IO.Directory.GetFiles(_mainViewModel.ModelsDirectory).Where(x => x.EndsWith(".zip") || x.EndsWith(".onnx")).Select(x => System.IO.Path.GetFileName(x)).ToList();
            ModelSelected = Models.FirstOrDefault();

            // Observables
            this.WhenAnyValue(x => x.ModelSelected)
                .WhereNotNull()
                .Subscribe(modelFilename =>
                {
                    _mainViewModel.LoadModel(modelFilename);
                });

            // Create command
            FolderViewSelect = ReactiveCommand.CreateFromTask(FolderViewSelectImpl);
            WebcamViewSelect = ReactiveCommand.CreateFromTask(WebcamViewSelectImpl);
        }

        private async Task FolderViewSelectImpl()
        {
            await _mainViewModel.Router.Navigate.Execute(Locator.Current.GetService<FolderViewModel>());
        }

        private async Task WebcamViewSelectImpl()
        {
            await _mainViewModel.Router.Navigate.Execute(Locator.Current.GetService<WebcamViewModel>());
        }
    }
}
