using InputKit.Shared.Controls;
using UraniumUI.Pages;

namespace ProjectDependencies;

public partial class MainPage : UraniumContentPage
{
    private MainPageViewModel _viewModel => (MainPageViewModel)this.BindingContext;
    private bool _showingPicker;
    private readonly IFolderPicker _folderPicker;

    public MainPage(IFolderPicker folderPicker)
    {
        SelectionView.GlobalSetting.CornerRadius = 0;
        InitializeComponent();
        _folderPicker = folderPicker;
    }

    private async void FilePicker_Clicked(object sender, EventArgs e)
    {
        if (_showingPicker)
        {
            return;
        }

        _showingPicker = true;

        try
        {
            var result = await _folderPicker.PickFolderAsync();

            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _viewModel.Directory = result;
            });
        }
        finally
        {
            _showingPicker = false;
        }
    }
}
