using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;

using MahApps.Metro.Controls;
using ReactiveUI;

namespace MachineLearning.ObjectDetect.WPF
{
    public class ReactiveMetroWindow<TViewModel> : MetroWindow, IViewFor<TViewModel> where TViewModel : class
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(TViewModel), typeof(ReactiveMetroWindow<TViewModel>), new PropertyMetadata(null));

        /// <summary>
        /// Gets the binding root view model.
        /// </summary>
        public TViewModel? BindingRoot => ViewModel;

        public TViewModel? ViewModel
        {
            get => (TViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (TViewModel?)value;
        }
    }
}
