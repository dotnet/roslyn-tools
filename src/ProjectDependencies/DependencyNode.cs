using NuGet.ProjectModel;
using System;
using System.Collections.ObjectModel;
using System.Security.Policy;

namespace ProjectDependencies;

public class DependencyNode
{
    public string DisplayName => ToString();

    public DependencyNode? Parent { get; }
    public string Name { get; }
    public string Version { get; }
    public string TypeKind { get; }
    public bool IsExpanded { 
        get; 
        set; 
    }
    public bool IsLeaf { get; set; }

    public ObservableCollection<DependencyNode> Children { get; } = new();

    public DependencyNode(string name, string version, string type, DependencyNode? parent)
    {
        Name = name;
        Version = version;
        TypeKind = type;
        Parent = parent;
    }

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
}
