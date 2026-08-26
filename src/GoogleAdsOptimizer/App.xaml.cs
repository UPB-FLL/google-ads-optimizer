using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleAdsOptimizer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set up dependency injection
            var serviceProvider = ConfigureServices();
            Current.Properties["ServiceProvider"] = serviceProvider;
        }

        private System.IServiceProvider ConfigureServices()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

            // Register Services
            services.AddSingleton<Services.GoogleAdsService>();
            services.AddSingleton<Services.GPTService>();
            services.AddSingleton<Services.CampaignAnalyzer>();
            services.AddSingleton<Services.UpdateService>();
            services.AddSingleton<Services.GoogleAdsExportService>();

            // Register ViewModels
            services.AddTransient<ViewModels.MainViewModel>();
            services.AddTransient<ViewModels.CampaignViewModel>();
            services.AddTransient<ViewModels.AdGeneratorViewModel>();
            services.AddTransient<ViewModels.SettingsViewModel>();

            return services.BuildServiceProvider();
        }
    }
}