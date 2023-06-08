using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Policy;

namespace ProjectDependencies;

public class DependencyNode : INotifyPropertyChanged
{
    public string DisplayName => ToString();

    public DependencyNode? Parent { get; }
    public string Name { get; }
    public string Version { get; }
    public string TypeKind { get; }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => Set(ref _isExpanded, value);
    }

    private bool _isLeaf;
    public bool IsLeaf
    {
        get => _isLeaf;
        set => Set(ref _isLeaf, value);
    }

    public ObservableCollection<DependencyNode> Children { get; } = new();

    public DependencyNode(string name, string version, string type, DependencyNode? parent)
    {
        Name = name;
        Version = version;
        TypeKind = type;
        Parent = parent;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void EnsureDependentNode(LockFileTargetLibrary library)
    {
        foreach (var node in Children)
        {
            if (library.Version.OriginalVersion == node.Version &&
                library.Name == node.Name)
            {
                return;
            }
        }

        Children.Add(new DependencyNode(library.Name, library.Version.OriginalVersion, library.Type, this));
    }

    public override int GetHashCode()
    {
        // Good enough? 🤷
        return ToString().GetHashCode();
    }

    public override string ToString()
    {
        return Name + ":" + Version;
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
