
using CommunityToolkit.Maui;
using InputKit.Shared.Controls;
using UraniumUI;

namespace ProjectDependencies;

public static class MauiProgram
{
	public static MauiApp Build(MauiAppBuilder builder)
	{
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

        return builder.Build();
	}
}
