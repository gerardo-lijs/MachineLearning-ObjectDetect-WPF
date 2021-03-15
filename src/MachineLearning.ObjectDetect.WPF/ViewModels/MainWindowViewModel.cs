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
        private OnnxOutputParser OutputParser { get; }
        private PredictionEngine<ImageInputData, CustomVisionPrediction>? CustomVisionPredictionEngine { get; }
        private PredictionEngine<ImageInputData, TinyYoloPrediction> TinyYoloPredictionEngine;
        private readonly string modelsDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, @"OnnxModels");

        public MainWindowViewModel()
        {
            Locator.CurrentMutable.RegisterConstant(this,typeof(IScreen));

            // Load Onnx model
            //var customVisionExport = System.IO.Directory.GetFiles(modelsDirectory, "*.zip").FirstOrDefault();

            //var customVisionModel = new CustomVisionModel(customVisionExport);
            //var modelConfigurator = new OnnxModelConfigurator(customVisionModel);

            //OutputParser = new OnnxOutputParser(customVisionModel);
            //CustomVisionPredictionEngine = modelConfigurator.GetMlNetPredictionEngine<CustomVisionPrediction>();

            var tinyYoloModel = new TinyYoloModel(System.IO.Path.Combine(modelsDirectory, "TinyYolo2_model.onnx"));
            var modelConfigurator = new OnnxModelConfigurator(tinyYoloModel);

            OutputParser = new OnnxOutputParser(tinyYoloModel);
            TinyYoloPredictionEngine = modelConfigurator.GetMlNetPredictionEngine<TinyYoloPrediction>();

            // Initialize the Router.
            Router = new RoutingState();

            // Start with New Capture content
            Router.Navigate.Execute(Locator.Current.GetService<FolderViewModel>());
        }

        public List<BoundingBox> DetectObjects(ImageInputData imageInputData)
        {
            var labels = CustomVisionPredictionEngine?.Predict(imageInputData).PredictedLabels ?? TinyYoloPredictionEngine?.Predict(imageInputData).PredictedLabels;
            var boundingBoxes = OutputParser.ParseOutputs(labels);
            var filteredBoxes = OutputParser.FilterBoundingBoxes(boundingBoxes, 5, 0.5f);
            return filteredBoxes;
        }
    }
}
