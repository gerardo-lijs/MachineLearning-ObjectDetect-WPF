using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Splat;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Microsoft.ML;
using OnnxObjectDetection;

namespace MachineLearning.ObjectDetect.WPF.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IScreen
    {
        public RoutingState Router { get; }

        // TODO: Change this to UI select folder and change model in runtime
        public OnnxOutputParser OutputParser { get; }
        public PredictionEngine<ImageInputData, CustomVisionPrediction> CustomVisionPredictionEngine { get; }
        private readonly string modelsDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, @"OnnxModels");

        public MainWindowViewModel()
        {
            Locator.CurrentMutable.RegisterConstant(this,typeof(IScreen));

            // Load Onnx model
            var customVisionExport = System.IO.Directory.GetFiles(modelsDirectory, "*.zip").FirstOrDefault();

            var customVisionModel = new CustomVisionModel(customVisionExport);
            var modelConfigurator = new OnnxModelConfigurator(customVisionModel);

            OutputParser = new OnnxOutputParser(customVisionModel);
            CustomVisionPredictionEngine = modelConfigurator.GetMlNetPredictionEngine<CustomVisionPrediction>();

            // Initialize the Router.
            Router = new RoutingState();

            // Start with New Capture content
            Router.Navigate.Execute(Locator.Current.GetService<FolderViewModel>());
        }
    }
}
