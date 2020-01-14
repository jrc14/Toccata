using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Toccata.Model;
using Toccata.ViewModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Core;

using Windows.UI.Xaml.Markup;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Toccata
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static MainPage Static = (MainPage)null;
        public static CoreDispatcher StaticDispatcher = (CoreDispatcher)null;

        public MainViewModel VM { get; set; }

        public MainPage()
        {
            MainPage.Static = this;

            this.InitializeComponent();

            this.VM = MainViewModel.Instance;

            MainPage.StaticDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            this.VM.Initialise();


            this.SizeChanged += MainPage_SizeChanged;
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double h = e.NewSize.Height;
            double w = e.NewSize.Width;
            double sz = Math.Min(h, w);
            if (sz <= 0.0)
                return;
            MainViewModel.Instance.PlayerOrientation = h <= w ? (Orientation)1 : (Orientation)0;
            if (MainViewModel.Instance.PlayerOrientation == 0)
            {
                double panelSize = Math.Min(sz - 16.0, h - 236.0);
                MainViewModel.Instance.PlayerPanelSize = panelSize;
                MainViewModel.Instance.QueuePanelHeight = h - (panelSize + 24.0);
                MainViewModel.Instance.QueuePanelWidth = panelSize;
            }
            else
            {
                double panelSize = Math.Min(sz - 16.0, w - 236.0);
                MainViewModel.Instance.PlayerPanelSize = panelSize;
                MainViewModel.Instance.QueuePanelHeight = panelSize;
                MainViewModel.Instance.QueuePanelWidth = w - (panelSize + 24.0);
            }
        }

        private void ArtistItemClick(object sender, ItemClickEventArgs e)
        {
            if (!(e.ClickedItem is FolderEntry clickedItem))
                return;
            if (clickedItem.IsFolder)
            {
                MainViewModel.Instance.OpenArtistFolder(clickedItem);
            }
            else
            {
                MainViewModel.Instance.AddTrackFileToQueue(clickedItem);
                MainViewModel.Instance.StartPlayingIfAppropriate();
            }
        }

        private void AlbumItemClick(object sender, ItemClickEventArgs e)
        {
            if (!(e.ClickedItem is FolderEntry clickedItem))
                return;
            if (clickedItem.IsFolder)
            {
                MainViewModel.Instance.OpenAlbumFolder(clickedItem);
            }
            else
            {
                MainViewModel.Instance.AddTrackFileToQueue(clickedItem);
                MainViewModel.Instance.StartPlayingIfAppropriate();
            }
        }

        private void TrackItemClick(object sender, ItemClickEventArgs e)
        {
            if (!(e.ClickedItem is FolderEntry clickedItem))
                return;
            if (clickedItem.IsFolder)
            {
                MainViewModel.Instance.OpenAlbumFolder(clickedItem);
            }
            else
            {
                MainViewModel.Instance.AddTrackFileToQueue(clickedItem);
                MainViewModel.Instance.StartPlayingIfAppropriate();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.Stop();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (this.btnPlay.Label == "play")
                MainViewModel.Instance.Play();
            else
                MainViewModel.Instance.Pause();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.Back();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.Next();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.AddTracks();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.ClearQueue();
        }

        public static void SetPlayButtonAppearance(bool showPauseLabel)
        {
            if (showPauseLabel)
            {
                MainPage.Static.btnPlay.Label="pause";
                MainPage.Static.btnPlay.Icon = new SymbolIcon(Symbol.Pause);
            }
            else
            {
                MainPage.Static.btnPlay.Label="play";
                MainPage.Static.btnPlay.Icon = new SymbolIcon(Symbol.Play);
            }
        }

        public static void SetSliderPosition(TimeSpan current, TimeSpan total)
        {
            if (MainPage._SliderIsBeingManipulated)
                return;

            double totalSeconds1 = current.TotalSeconds;
            double totalSeconds2 = total.TotalSeconds;

            MainPage.Static.slPosition.Minimum=0;
            MainPage.Static.slPosition.Value = current.TotalSeconds;
            MainPage.Static.slPosition.Maximum = total.TotalSeconds;

            double num1 = 100.0 * totalSeconds1 / totalSeconds2;
            if (num1 < 0.1 || num1 > 99.9)
            {
                MainPage.Static.tbPosition.Text="";
            }
            else
            {
                int m = (int)(totalSeconds1 / 60.0);
                int s = (int)(totalSeconds1 - (double)(60 * m));
                MainPage.Static.tbPosition.Text=string.Format("{0:00}:{1:00}", (object)m, (object)s);
            }
        }

        public static void SetNowPlaying(string s)
        {
            MainPage.Static.tbNowPlaying.Text=s;
        }

        private static bool _WasPlayingWhenManipulationStarted = false;
        private static bool _SliderIsBeingManipulated = false;
        private void slPosition_SliderManipulationStarted(object sender, EventArgs e)
        {
            MainPage._SliderIsBeingManipulated = true;

            if (ToccataModel.MediaPlayerIsPlaying())
                MainPage._WasPlayingWhenManipulationStarted = true;

            MainViewModel.Instance.Pause();
        }

        private void slPosition_SliderManipulationCompleted(object sender, EventArgs e)
        {
            if (sender is ToccataSlider toccataSlider)
            {
                MainViewModel.Instance.SetPlayerPosition(TimeSpan.FromSeconds(toccataSlider.Value));

                if (MainPage._WasPlayingWhenManipulationStarted)
                    MainViewModel.Instance.Play();

                MainPage._WasPlayingWhenManipulationStarted = false;
            }
            MainPage._SliderIsBeingManipulated = false;
        }

        private void PlayQueue_ItemTapped(object sender, TappedRoutedEventArgs e)
        {
            PlayableItem i = (sender as Grid).Tag as PlayableItem;

            if (i == null)
                return;

            MenuFlyout menuFlyout = new MenuFlyout();

            string str = i.DisplayName;
            if (str.Length > 15)
                str = str.Substring(0, 14) + "...";

            MenuFlyoutItem miDelete = new MenuFlyoutItem();
            menuFlyout.Items.Add(miDelete);
            miDelete.Text="delete '" + str + "'";
            miDelete.Click += (object ss, RoutedEventArgs ee)=> { MainViewModel.Instance.DeleteFromQueue(i); };


            MenuFlyoutItem miNext = new MenuFlyoutItem();
            menuFlyout.Items.Add(miNext);
            miNext.Text = "play '" + str + "' next";
            miNext.Click += (object ss, RoutedEventArgs ee) => { MainViewModel.Instance.PlayNext(i); };

            MenuFlyoutItem miNow = new MenuFlyoutItem();
            menuFlyout.Items.Add(miNow);
            miNow.Text = "play '" + str + "' now";
            miNow.Click += (object ss, RoutedEventArgs ee) => { MainViewModel.Instance.PlayNow(i); };

            menuFlyout.ShowAt(sender as Grid);
        }

        private void PlayQueue_ItemHolding(object sender, HoldingRoutedEventArgs e)
        {
            this.PlayQueue_ItemTapped(sender, new TappedRoutedEventArgs());
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation=(PickerLocationId)0;
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt"});
            savePicker.SuggestedFileName="playlist";
            StorageFile file = (StorageFile)null;

            try
            {
                file = await savePicker.PickSaveFileAsync();
            }
            catch (Exception ex)
            {
            }
            if (file == null)
                return;

            CachedFileManager.DeferUpdates((IStorageFile)file);
            
            await MainViewModel.Instance.SavePlayQueue(file);

            FileUpdateStatus status =  await CachedFileManager.CompleteUpdatesAsync(file);
        }

        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode=(PickerViewMode)0;
            openPicker.SuggestedStartLocation=(PickerLocationId)0;
            openPicker.FileTypeFilter.Add(".txt");
            StorageFile file = (StorageFile)null;
            try
            {
                file = await openPicker.PickSingleFileAsync();
            }
            catch (Exception ex)
            {
            }
            if (file == null)
                return;
            
            await MainViewModel.Instance.LoadPlayQueue(file);
        }

        private void btnShuffle_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.ShufflePlayQueue();
        }
    }
}
