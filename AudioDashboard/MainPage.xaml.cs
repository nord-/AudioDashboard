﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI;
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
            foreach (var file in clickedItem.Files)
            {
                // load files into memory
                var memoryStream = new InMemoryRandomAccessStream();
                using (var inputStream = await file.File.OpenReadAsync())
                {
                    await RandomAccessStream.CopyAsync(inputStream, memoryStream);
                }

                // <Button Content     ="Name" Height="150" Width="150" Background="Red" Foreground="White" FontSize="24" />
                var button             = new Button { Height = 150, Width = 150, FontSize = 24 };
                // <TextBlock Text     ="Customer Locations" TextWrapping="Wrap" />
                button.Content         = new TextBlock { Text = file.Name, TextWrapping = TextWrapping.WrapWholeWords };
                button.Background      = clickedItem.BackgroundColor;
                button.Foreground      = InvertColor(clickedItem.BackgroundColor.Color);
                button.BorderBrush     = new SolidColorBrush(Colors.White);
                button.Tag             = memoryStream;
                button.Click          += AudioButton_Click;
                button.ContextRequested += AudioButton_ContextRequested;
                AudioButtonGrid.Children.Add(button);
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

        //private SolidColorBrush InvertColor(Color color)
        //{
        //    var newColorBrush = new SolidColorBrush( Color.FromArgb(255, Convert.ToByte(255-color.R), Convert.ToByte(255-color.G),Convert.ToByte(255-color.B)));
        //    return newColorBrush;
        //}

        public SolidColorBrush InvertColor(Color c)
        {

            //var l = 0.2126 * c.ScR + 0.7152 * c.ScG + 0.0722 * c.ScB;
            //return l < 0.5 ? Brushes.White : Brushes.Black;
            double Y = 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;
            var color = (Y/255.0) > 0.5 ? Colors.Black : Colors.White;
            //var color = Color.FromArgb(255, c.R > 127 ? (byte)0 : (byte)255, c.G > 127 ? (byte)0 : (byte)255, c.B > 127 ? (byte)0 : (byte)255);
            return new SolidColorBrush(color);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await GetFoldersAsync();
        }
    }
}
