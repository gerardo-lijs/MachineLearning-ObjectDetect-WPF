using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Reactive.Disposables;
using System.Windows.Controls;
using System.Windows.Media;
using System.Reactive;
using System.Reactive.Linq;

using ReactiveUI;

using MachineLearning.ObjectDetect.WPF.ViewModels;

namespace MachineLearning.ObjectDetect.WPF.Views
{
    public class MainWindowBase : ReactiveMetroWindow<MainWindowViewModel> { }
    public partial class MainWindow : MainWindowBase
    {
        public MainWindow()
        {
            ViewModel = new MainWindowViewModel();
            InitializeComponent();
            this.WhenActivated(disposableRegistration =>
            {
                this.WhenAnyValue(viewModel => viewModel.ViewModel).BindTo(this, view => view.DataContext).DisposeWith(disposableRegistration);

                // Router
                this.OneWayBind(ViewModel, viewModel => viewModel.Router, view => view.RoutedViewHost.Router).DisposeWith(disposableRegistration);
            });
        }
    }
}
