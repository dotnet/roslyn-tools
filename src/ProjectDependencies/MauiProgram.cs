
using CommunityToolkit.Maui;
using InputKit.Shared.Controls;
using UraniumUI;

namespace ProjectDependencies;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
        var builder = MauiApp.CreateBuilder();

		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseUraniumUI()
			.UseUraniumUIMaterial()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

				fonts.AddMaterialIconFonts();
            });

#if WINDOWS
        builder.Services.AddTransient<IFolderPicker, ProjectDependencies.Platforms.Windows.WindowsFolderPicker>();
#endif

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<App>();

        return builder.Build();
	}
}
