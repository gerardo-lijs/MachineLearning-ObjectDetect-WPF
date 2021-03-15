using System;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace MachineLearning.ObjectDetect.WPF
{
    public partial class App : Application
    {
        public IHost AppHost { get; }

        public App()
        {
            // Build AppHost
            AppHost = Host
                .CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .Build();
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            await AppHost.StartAsync();

            // Show main window
            StartupUri = new Uri("Views/MainWindow.xaml", UriKind.Relative);
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            using (AppHost)
            {
                await AppHost.StopAsync(TimeSpan.FromSeconds(5));
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // RxUI uses Splat as its default DI engine but we can instruct it to use Microsoft DI instead
            services.UseMicrosoftDependencyResolver();
            var resolver = Locator.CurrentMutable;
            resolver.InitializeSplat();
            resolver.InitializeReactiveUI();

            // Manual register ViewModels and Windows
            services.AddTransient<Views.MainWindow>();
            services.AddTransient<ViewModels.MainWindowViewModel>();

            services.AddTransient<ViewModels.SelectViewModel>();
            services.AddTransient<ViewModels.FolderViewModel>();
            services.AddTransient<ViewModels.WebcamViewModel>();

            // Manual register views
            services.AddTransient(typeof(IViewFor<ViewModels.SelectViewModel>), typeof(Views.SelectView));
            services.AddTransient(typeof(IViewFor<ViewModels.FolderViewModel>), typeof(Views.FolderView));
            services.AddTransient(typeof(IViewFor<ViewModels.WebcamViewModel>), typeof(Views.WebcamView));
        }
    }
}
