using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace ProjectDependencies
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private CancellationTokenSource _dependenciesCancellationSource = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private string _path = "";
        public string Path
        {
            get => _path;
            set => Set(ref _path, value);
        }

        private string _packageName = "";
        public string PackageName
        {
            get => _packageName;
            set => Set(ref _packageName, value);
        }

        private string _packageVersion = "";
        public string PackageVersion
        {
            get => _packageVersion;
            set => Set(ref _packageVersion, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        private void FilePicker_Clicked(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Path = dialog.SelectedPath;
            }
        }

        private void Calculate_Clicked(object sender, RoutedEventArgs e)
        {
            _dependenciesCancellationSource.Cancel();
            _dependenciesCancellationSource = new();

            var cancellationToken = _dependenciesCancellationSource.Token;

            try
            {
                DependenciesTree.Visibility = Visibility.Collapsed;
                StatusText.Visibility = Visibility.Visible;

                DependenciesTree.Items.Clear();
                var nodes = BuildDependencyFinder.FindDependencies(
                    Path,
                    PackageName,
                    PackageVersion,
                    (count, finished) =>
                    {
                        StatusText.Text = $"Loading from {count} files";
                    },
                    cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                foreach (var node in nodes)
                {
                    DependenciesTree.Items.Add(node);
                }
            }
            finally
            {
                DependenciesTree.Visibility = Visibility.Visible;
                StatusText.Visibility = Visibility.Collapsed;
            }

            
        }
    }
}
