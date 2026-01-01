using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Reflection;

namespace ControlCenter.Core.ViewModels;

/// <summary>
/// ViewModel per AboutPage
/// </summary>
public partial class AboutViewModel : ObservableObject
{
    public AboutViewModel()
    {
        LoadVersionInfo();
    }

    [ObservableProperty]
    private string _version = "1.0.0";

    [ObservableProperty]
    private string _buildDate = string.Empty;

    [ObservableProperty]
    private string _frameworkVersion = string.Empty;

    private void LoadVersionInfo()
    {
        try
        {
            // Ottieni versione dall'assembly
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            Version = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

            // Ottieni data build (approssimativa)
            var buildDateTime = new DateTime(2000, 1, 1)
                .AddDays(version?.Build ?? 0)
                .AddSeconds((version?.Revision ?? 0) * 2);
            BuildDate = buildDateTime.ToString("dd MMMM yyyy");

            // Framework version
            FrameworkVersion = Environment.Version.ToString();
        }
        catch
        {
            Version = "1.0.0";
            BuildDate = "2025";
            FrameworkVersion = ".NET 8";
        }
    }
}
