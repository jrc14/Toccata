/*
Toccata Reader, including all its source files, is licenced under the MIT Licence:

 Copyright (c) 2020 Turnipsoft Ltd, Jim Chapman

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
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
    /// The main, and only, UI page for the app.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static CoreDispatcher StaticDispatcher = null; // so background threads can invoke stuff on the UI thread if they need to.

        public MainViewModel VM { get; set; } // to give a handy way for XAML to bind to the viewmodel.

        private static MainPage Static = null; // so my static members can get at the MainPage.

        public MainPage()
        {
            MainPage.Static = this;

            this.InitializeComponent();

            this.VM = MainViewModel.Instance;

            MainPage.StaticDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            this.VM.Initialise(); // Create the viewmodel's lists of artists, albums and tracks, and the play queue , and populate the list of artists.


            this.SizeChanged += MainPage_SizeChanged;
        }

        /// <summary>
        /// When the main page's size is set, I figure out the proper dimensions for the player panel and the queue panel, and their orientation,
        /// and publish this via bindable properties.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            if (!(e.ClickedItem is FolderEntry clickedItem)) // should never happen
                return;

            if (clickedItem.IsFolder)
            {
                MainViewModel.Instance.OpenArtistFolder(clickedItem); // click on an artist folder == populate the album folder, using its contents
            }
            else
            {
                MainViewModel.Instance.AddTrackFileToQueue(clickedItem); // click on a track = queue it to play
                MainViewModel.Instance.StartPlayingIfAppropriate(); // and play it, if we aren't already playing something
            }
        }

        private void AlbumItemClick(object sender, ItemClickEventArgs e)
        {
            if (!(e.ClickedItem is FolderEntry clickedItem)) // should never happen
                return;

            if (clickedItem.IsFolder)
            {
                MainViewModel.Instance.OpenAlbumFolder(clickedItem); // click on an album folder == populate the tracks folder, using its contents
            }
            else
            {
                MainViewModel.Instance.AddTrackFileToQueue(clickedItem); // click on a track = queue it to play
                MainViewModel.Instance.StartPlayingIfAppropriate(); // and play it, if we aren't already playing something
            }
        }

        private void TrackItemClick(object sender, ItemClickEventArgs e)
        {
            if (!(e.ClickedItem is FolderEntry clickedItem)) // should never happen
                return;

            if (clickedItem.IsFolder)
            {
                MainViewModel.Instance.OpenAlbumFolder(clickedItem); // click on a folder in the tracks list == treat it as an album, i.e. populate the tracks folder, using its contents
            }
            else
            {
                MainViewModel.Instance.AddTrackFileToQueue(clickedItem); // click on a track = queue it to play
                MainViewModel.Instance.StartPlayingIfAppropriate(); // and play it, if we aren't already playing something
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.Stop(); // hard-stop the player
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

        public static void SetPlayButtonAppearance(bool showPauseLabel) // called when the player's state changes.
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

        /// <summary>
        /// Call this method when the player determines that the UI's position slider should be moved, because of progress playing the track.
        /// </summary>
        /// <param name="current">how much of the track has been played</param>
        /// <param name="total">how long the track is</param>
        public static void SetSliderPosition(TimeSpan current, TimeSpan total) // called when the player reports a change in position
        {
            if (MainPage._SliderIsBeingManipulated) // ignore position changes that happen while the slider UI is being manipulated by the user
                return;

            double cs = current.TotalSeconds;
            double ts = total.TotalSeconds;

            MainPage.Static.slPosition.Minimum=0;
            MainPage.Static.slPosition.Value = current.TotalSeconds;
            MainPage.Static.slPosition.Maximum = total.TotalSeconds;

            double num1 = 100.0 * cs / ts;
            if (num1 < 0.1 || num1 > 99.9) // if at/near the start or end of the track, blank the position label ...
            {
                MainPage.Static.tbPosition.Text=""; 
            }
            else // otherwise display minutes & seconds in the position label.
            {
                int m = (int)(cs / 60.0);
                int s = (int)(cs - (double)(60 * m));
                MainPage.Static.tbPosition.Text=string.Format("{0:00}:{1:00}", (object)m, (object)s);
            }
        }

        /// <summary>
        /// Call this method when the player has detected a change to what track (or no track) is being played.
        /// </summary>
        /// <param name="s">text to display, to show the details of the track that is playing</param>
        public static void SetNowPlaying(string s)
        {
            MainPage.Static.tbNowPlaying.Text=s; // set the label that shows track details
        }

        private static bool _WasPlayingWhenManipulationStarted = false; // was music playing, before the user started touching the slider?
        private static bool _SliderIsBeingManipulated = false; // set to true while the user is manually manipulating the slider
        private void slPosition_SliderManipulationStarted(object sender, EventArgs e)
        {
            MainPage._SliderIsBeingManipulated = true;

            if (ToccataModel.MediaPlayerIsPlaying())
                MainPage._WasPlayingWhenManipulationStarted = true;

            MainViewModel.Instance.Pause();
        }

        private void slPosition_SliderManipulationCompleted(object sender, EventArgs e)
        {
            if (sender is ToccataSlider toccataSlider) // should always be true
            {
                MainViewModel.Instance.SetPlayerPosition(TimeSpan.FromSeconds(toccataSlider.Value)); // when the user has finished moving the slider, update the player position according to where they put the slider

                if (MainPage._WasPlayingWhenManipulationStarted) // resume play, if we were playing music before the slider manipulation began
                    MainViewModel.Instance.Play();

                MainPage._WasPlayingWhenManipulationStarted = false;

                MainPage._SliderIsBeingManipulated = false;
            }

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
            savePicker.SuggestedStartLocation=PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt"});
            savePicker.SuggestedFileName="playlist";

            StorageFile file = null;

            try
            {
                file = await savePicker.PickSaveFileAsync();
            }
            catch (Exception )
            {
            }
            if (file == null)
                return;

            CachedFileManager.DeferUpdates(file); // UWP boilerplate
            
            await MainViewModel.Instance.SavePlayQueue(file); // save the current play queue to the file

            FileUpdateStatus status =  await CachedFileManager.CompleteUpdatesAsync(file); // UWP boilerplate
        }

        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode=PickerViewMode.List;
            openPicker.SuggestedStartLocation=PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".txt");

            StorageFile file = null;
            try
            {
                file = await openPicker.PickSingleFileAsync();
            }
            catch (Exception )
            {
            }
            if (file == null)
                return;
            
            await MainViewModel.Instance.LoadPlayQueue(file); // read play queue items from the file, and add them to the bottom of the play queue
        }

        private void btnShuffle_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.ShufflePlayQueue(); // sort the play queue into a random order
        }
    }
}
