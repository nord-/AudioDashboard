using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace AudioDashboard
{
    public sealed partial class MainPage : Page
    {
        private const string FolderToken = "PickedFolderToken";
        public ObservableCollection<AudioFolder> FolderLinks { get; private set; } = new ObservableCollection<AudioFolder>();
        private string _fileLoaded = "";

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

        private async void NavLinksList_ItemClick(object sender, ItemClickEventArgs e)
        {
            await RefreshAudioFiles((AudioFolder)e.ClickedItem);
        }

        private async Task RefreshAudioFiles(AudioFolder clickedItem)
        {
            AudioButtonGrid.Children.Clear();
            if (clickedItem != null)
            {
                foreach (var file in clickedItem.Files)
                {
                    // load files into memory
                    var memoryStream = new InMemoryRandomAccessStream();
                    using (var inputStream = await file.File.OpenReadAsync())
                    {
                        await RandomAccessStream.CopyAsync(inputStream, memoryStream);
                    }

                    // <Button Content     ="Name" Height="150" Width="150" Background="Red" Foreground="White" FontSize="24" />
                    var button = new Button { Height = 150, Width = 150, FontSize = 24 };
                    // <TextBlock Text     ="Customer Locations" TextWrapping="Wrap" />
                    button.Content = new TextBlock { Text = file.Name, TextWrapping = TextWrapping.WrapWholeWords };
                    button.Background = clickedItem.BackgroundColor;
                    button.Foreground = InvertColor(clickedItem.BackgroundColor.Color);
                    button.BorderBrush = new SolidColorBrush(Colors.White);
                    button.Tag = memoryStream;
                    button.Click += AudioButton_Click;
                    button.ContextRequested += AudioButton_ContextRequested;
                    AudioButtonGrid.Children.Add(button);
                }
            }
        }

        private void AudioButton_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var b = sender as Button;
            LoadMediaFromTag(b);
            Debug.WriteLine(_fileLoaded);
        }

        private void LoadMediaFromTag(Button b)
        {
            AudioMediaPlayer.AutoPlay = false;
            AudioMediaPlayer.Source = MediaSource.CreateFromStream((InMemoryRandomAccessStream)b.Tag, "audio/mpeg");
            _fileLoaded = ((TextBlock)b.Content).Text;
        }

        private void AudioButton_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            Debug.WriteLine($"{_fileLoaded} {((TextBlock)b.Content).Text}");

            if (AudioMediaPlayer.Source == null || ((MediaSource)AudioMediaPlayer.Source).State != MediaSourceState.Opened
                || _fileLoaded != ((TextBlock)b.Content).Text)
            {
                LoadMediaFromTag(b);
            }

            AudioMediaPlayer.MediaPlayer.Play();
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
                        Extension = p.FileType,
                        File      = p
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

        public SolidColorBrush InvertColor(Color c)
        {
            double Y = 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;
            var color = (Y/255.0) > 0.5 ? Colors.Black : Colors.White;

            return new SolidColorBrush(color);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await GetFoldersAsync();
            await RefreshAudioFiles(null);
        }

        private async void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            await SelectBaseFolder();
        }
    }
}
