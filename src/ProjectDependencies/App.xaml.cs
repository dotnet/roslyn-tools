using UraniumUI.Material.Resources;

namespace ProjectDependencies;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new AppShell();
	}
}
