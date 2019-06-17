using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AudioDashboard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string FolderToken = "PickedFolderToken";
        public ObservableCollection<AudioFolder> FolderLinks { get; private set; } = new ObservableCollection<AudioFolder>();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await GetFoldersAsync();

            if (!FolderLinks.Any())
            {
                await SelectBaseFolder();
            }
        }

        private async void Audio1button_Click(object sender, RoutedEventArgs e)
        {
            await SelectBaseFolder();
        }

        private async Task GetFoldersAsync()
        {
            if (!Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.ContainsItem(FolderToken))
                return;

            FolderLinks.Clear();

            var baseFolder   = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(FolderToken);
            var folders      = (await baseFolder.GetFoldersAsync());

            foreach (var f in folders)
            {
                var files  = await f.GetFilesAsync();
                var folder = new AudioFolder
                {
                    Name  = f.Name,
                    Path  = f.Path,
                    Files = files.Select(p => new AudioFile
                    {
                        Name      = p.Name,
                        Path      = p.Path,
                        Extension = p.FileType
                    }).ToList()
                };
                FolderLinks.Add(folder);
            }
        }

        private async Task SelectBaseFolder()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace(FolderToken, folder);
                await GetFoldersAsync();
            }
            else
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove(FolderToken);
            }
        }
    }
}
