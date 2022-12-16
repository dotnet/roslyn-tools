// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Windows.Storage.Pickers;

namespace ProjectDependencies.Platforms.Windows
{
    internal class WindowsFolderPicker : IFolderPicker
    {
        public async Task<string> PickFolderAsync()
        {
            var picker = new FolderPicker();

            // Get the current window's HWND by passing in the Window object
            var hwnd = ((MauiWinUIWindow)Application.Current!.Windows[0].Handler.PlatformView!).WindowHandle;

            // Associate the HWND with the file picker
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var result = await picker.PickSingleFolderAsync();

            return result.Path;
        }
    }
}
