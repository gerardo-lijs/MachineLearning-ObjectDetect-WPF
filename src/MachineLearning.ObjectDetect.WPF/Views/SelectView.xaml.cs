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
    public class SelectViewBase : ReactiveUserControl<SelectViewModel> { }
    public partial class SelectView : SelectViewBase
    {
        public SelectView()
        {
            InitializeComponent();
            this.WhenActivated(disposableRegistration =>
            {
                this.WhenAnyValue(viewModel => viewModel.ViewModel).BindTo(this, view => view.DataContext).DisposeWith(disposableRegistration);

                // Model combo
                ModelCombobox.DisplayMemberPath = ".";
                this.OneWayBind(ViewModel, viewModel => viewModel.Models, view => view.ModelCombobox.ItemsSource).DisposeWith(disposableRegistration);
                this.Bind(ViewModel, viewModel => viewModel.ModelSelected, view => view.ModelCombobox.SelectedValue).DisposeWith(disposableRegistration);

                // Commands
                this.BindCommand(ViewModel, viewModel => viewModel.FolderViewSelect, view => view.FolderViewButton).DisposeWith(disposableRegistration);
                this.BindCommand(ViewModel, viewModel => viewModel.WebcamViewSelect, view => view.WebcamViewButton).DisposeWith(disposableRegistration);

            });
        }
    }
}
