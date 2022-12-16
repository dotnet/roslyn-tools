using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using UraniumUI;

namespace ProjectDependencies;

public class MainPageViewModel : BindableObject
{
    public ObservableCollection<DependencyNode> Nodes { get; } = new();

    private string? _directory;
    public string? Directory
    {
        get => _directory;
        set
        {
            if (value != _directory)
            {
                _directory = value;
                OnPropertyChanged(nameof(Directory));
            }
        }
    }

    public string? PackageName { get; set; }
    public string? PackageVersion { get; set; }

    public ICommand CalculateTreeCommand { get; }

    private CancellationTokenSource _calculateTreeCancellationSource = new();

    private bool _loading;
    public bool Loading
    {
        get => _loading;
        set
        {
            if (value != _loading)
            {
                _loading = value;
                OnPropertyChanged(nameof(Loading));
            }
        }
    }

    private string? _loadingText;
    public string? LoadingText
    {
        get => _loadingText;
        set
        {
            if (value != _loadingText)
            {
                _loadingText = value;
                OnPropertyChanged(nameof(LoadingText));
            }
        }
    }

    public MainPageViewModel()
    {
        CalculateTreeCommand = new Command(async () =>
        {
            _calculateTreeCancellationSource.Cancel();
            _calculateTreeCancellationSource = new();

            if (Directory is null)
            {
                // TODO
                return;
            }

            if (PackageName is null)
            {
                // TODO
                return;
            }

            if (PackageVersion is null)
            {
                // TODO
                return;
            }

            Debug.Assert(MainThread.IsMainThread);

            Loading = true;

            var cancellationToken = _calculateTreeCancellationSource.Token;
            var nodesTask = BuildDependencyFinder.FindDependenciesAsync(
                Directory, 
                PackageName,
                PackageVersion, 
                (count, finished) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (finished)
                        {
                            LoadingText = $"Building dependency tree from {count} files";
                        }
                        else
                        {
                            LoadingText = $"Loading from {count} files";
                        }
                    });
                },
                cancellationToken);

            _ = nodesTask.ContinueWith(async t =>
            {
                var nodes = nodesTask.Result;
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Nodes.Clear();
                    foreach (var node in nodes)
                    {
                        Nodes.Add(node);
                    }

                    Loading = false;
                });
            });
        });
    }
}
