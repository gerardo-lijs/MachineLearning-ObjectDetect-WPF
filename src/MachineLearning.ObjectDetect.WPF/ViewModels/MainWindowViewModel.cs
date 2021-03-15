using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
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

        public string ModelsDirectory { get; }

        private OnnxOutputParser? OutputParser;
        private PredictionEngine<ImageInputData, CustomVisionPrediction>? CustomVisionPredictionEngine;
        private PredictionEngine<ImageInputData, TinyYoloPrediction>? TinyYoloPredictionEngine;

        public MainWindowViewModel()
        {
            Locator.CurrentMutable.RegisterConstant(this, typeof(IScreen));

            ModelsDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, @"OnnxModels");

            // Initialize the Router.
            Router = new RoutingState();
        }

        public void LoadModel(string modelFilename)
        {
            var modelFullFilename = System.IO.Path.Combine(ModelsDirectory, modelFilename);

            // Load Onnx model
            if (modelFilename.EndsWith(".zip"))
            {
                var customVisionModel = new CustomVisionModel(modelFullFilename);
                var modelConfigurator = new OnnxModelConfigurator(customVisionModel);

                OutputParser = new OnnxOutputParser(customVisionModel);
                CustomVisionPredictionEngine = modelConfigurator.GetMlNetPredictionEngine<CustomVisionPrediction>();
                TinyYoloPredictionEngine = null;
            }
            else
            {
                var tinyYoloModel = new TinyYoloModel(modelFullFilename);
                var modelConfigurator = new OnnxModelConfigurator(tinyYoloModel);

                OutputParser = new OnnxOutputParser(tinyYoloModel);
                TinyYoloPredictionEngine = modelConfigurator.GetMlNetPredictionEngine<TinyYoloPrediction>();
                CustomVisionPredictionEngine = null;
            }
        }

        public List<BoundingBox> DetectObjects(ImageInputData imageInputData)
        {
            if (OutputParser is null) throw new Exception("Model not loaded.");

            var labels = CustomVisionPredictionEngine?.Predict(imageInputData).PredictedLabels ?? TinyYoloPredictionEngine?.Predict(imageInputData).PredictedLabels;
            var boundingBoxes = OutputParser.ParseOutputs(labels);
            var filteredBoxes = OutputParser.FilterBoundingBoxes(boundingBoxes, 5, 0.5f);
            return filteredBoxes;
        }
    }
}
