using NuGet.ProjectModel;
using System.Collections.ObjectModel;

namespace ProjectDependencies;

public class DependencyNode
{
    public DependencyNode? Parent { get; init; }
    public string Name { get; init; }
    public string Version { get; init; }
    public string TypeKind { get; init; }
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
        return HashCode.Combine(
            HashCode.Combine(Name.GetHashCode(),
            HashCode.Combine(Version.GetHashCode(),
            HashCode.Combine(TypeKind.GetHashCode(), Parent?.GetHashCode() ?? 0))));
    }

    public override string ToString()
    {
        return Name + ":" + Version;
    }
}
