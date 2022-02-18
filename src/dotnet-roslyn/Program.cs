using Microsoft.Roslyn.Tool.Commands;
using System.CommandLine;

return await RootRoslynCommand.GetRootCommand().InvokeAsync(args);
