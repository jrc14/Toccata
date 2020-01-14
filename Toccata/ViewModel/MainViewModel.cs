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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Toccata.Model;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace Toccata.ViewModel
{
    /// <summary>
    /// A singleton viewmodel class, containing the 'business logic' that connects the UI to the mediaplayer.  Accessed via the
    /// static variable, MainViewModel.Instance.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {

        private static MainViewModel _Instance = new MainViewModel();
        /// <summary>
        /// The one and only instance of the viewmodel.
        /// </summary>
        public static MainViewModel Instance
        {
            get
            {
                return MainViewModel._Instance;
            }
        }

        /// <summary>
        /// Call this method before trying to play music.
        /// Sets up and reads the root folder (where the list of artists is).  At present, this is hard-coded to the user's top-level Music folder.
        /// Also sets up the media player.
        /// </summary>
        public void Initialise()
        {
            this.RootFolder = KnownFolders.MusicLibrary; // a side-effect in the accessor will cause the folder to be read in to the Artists list.
            ToccataModel.SetUpMediaPlayer();
        }

        /// <summary>
        /// Clears the existing lists of albums and tracks, and loads a new list of albums for a given artist
        /// </summary>
        /// <param name="f">the folder whose name is the artist name</param>
        public async void OpenArtistFolder(FolderEntry f)
        {
            this.Albums.Clear();
            this.Tracks.Clear();

            await ToccataModel.PopulateFolderItems(this.Albums, f.storage as StorageFolder);
        }

        /// <summary>
        /// Clears the existing lists of tracks, and loads a new list of tracks for a given album
        /// </summary>
        /// <param name="f">the folder whose name is the album name</param>
        public async void OpenAlbumFolder(FolderEntry f)
        {
            this.Tracks.Clear();

            await ToccataModel.PopulateFolderItems(this.Tracks, f.storage as StorageFolder);
            if (this.PlayQueue.Count != 0)
                return;
            this.AddTracks();
        }

        /// <summary>
        /// Adds a track to the bottom of the play queue
        /// </summary>
        /// <param name="f">the track file to add</param>
        public void AddTrackFileToQueue(FolderEntry f)
        {
            if (f.IsFolder)
                return;
            this.PlayQueue.Add(new PlayableItem(f.storage as StorageFile));
        }

        /// <summary>
        /// Stops the media player (it puts it into the 'hard-stopped' state)
        /// </summary>
        public void Stop()
        {
            ToccataModel.Stop();
        }

        /// <summary>
        /// Starts or restarts playback.
        /// </summary>
        public void Play()
        {
            if (ToccataModel.MediaPlayerHasNoSource()) // if the player has been hard-stopeed, then ...
            {
                if (this.PlayQueue.Count <= 0)
                    return;
                ToccataModel.Play(this.PlayQueue[0].storage); // ... play the top track on the queue
            }
            else
                ToccataModel.Play(); // ... else play whatever is currently assigned as the player's source.
        }

        /// <summary>
        /// Pauses playback (which is to say, stops it, but does not hard-stop it)
        /// </summary>
        public void Pause()
        {
            ToccataModel.Pause();
        }

        private List<PlayableItem> history = new List<PlayableItem>(); // tracks we have finished (so the back button can go back to them)
        /// <summary>
        /// Stops playing the current track (if it's playing) and goes back to the start of the previous track we were playing.
        /// </summary>
        public void Back()
        {
            if (this.history.Count <= 0)
                return;

            bool wasPlayingBefore = false;

            if (ToccataModel.MediaPlayerIsPlaying())
                wasPlayingBefore = true;

            this.Stop();
            this.PlayQueue.Insert(0, this.history[0]); // Put the top track from ths history list onto the top of the play queue,
            this.history.RemoveAt(0); // and remove it from the history list

            if (wasPlayingBefore)
                this.StartPlayingIfAppropriate();
        }

        /// <summary>
        /// Stop playing the current track and move it to the history, then start playing the track that is now the top of the play queue.
        /// </summary>
        public void Next()
        {
            if (this.PlayQueue.Count <= 1) // if there is no queued track, do nothing.  If there is only one queued track, then there is no next track, so do nothing.
                return;

            bool wasPlayingBefore = false;

            if (ToccataModel.MediaPlayerIsPlaying())
                wasPlayingBefore = true;

            this.Stop();
            this.history.Insert(0, this.PlayQueue[0]); // Take the currently playing track (the one at the top of the queue), add it to the history
            this.PlayQueue.RemoveAt(0); // and remove it from the play queue.

            if (wasPlayingBefore)
                this.StartPlayingIfAppropriate();
        }

        /// <summary>
        /// Take all the tracks that are in the Tracks list, and add them to the bottom of the play queue.  If there are any
        /// folders in the list, they are ignored.
        /// </summary>
        public void AddTracks()
        {
            foreach (FolderEntry track in (Collection<FolderEntry>)this.Tracks)
                this.AddTrackFileToQueue(track);
            this.StartPlayingIfAppropriate();
        }

        /// <summary>
        /// Start playing, from the start of the track, if: (1) there is a queued track to play and (2) the player is in the hard-stopped state at the moment
        /// </summary>
        public void StartPlayingIfAppropriate()
        {
            if (this.PlayQueue.Count == 0 || !ToccataModel.MediaPlayerHasNoSource())
                return;

            ToccataModel.Play(this.PlayQueue[0].storage); // start playing the track that is at the top of the queue.
        }

        /// <summary>
        /// Stop playback and empty the play queue.
        /// </summary>
        public void ClearQueue()
        {
            ToccataModel.Stop();
            this.PlayQueue.Clear();
        }

        /// <summary>
        /// Set the playback position.
        /// </summary>
        /// <param name="t"></param>
        public void SetPlayerPosition(TimeSpan t)
        {
            ToccataModel.SetPlayerPosition(t);
        }

        /// <summary>
        /// Remove an item from the play queue.  If it's the top item of the queue, and currently playing, then stop playing it, and
        /// start playing the next on the queue.
        /// </summary>
        /// <param name="i">item to remove</param>
        public void DeleteFromQueue(PlayableItem i)
        {
            if (this.PlayQueue.Count <= 0)
                return;

            bool wasPlayingBefore = false;
            if (ToccataModel.MediaPlayerIsPlaying())
                wasPlayingBefore = true;

            if (this.PlayQueue[0] == i) // if we're removing the top item of the queue (i.e. the current track) 
                this.Stop();

            this.PlayQueue.Remove(i);

            if (wasPlayingBefore)
                this.StartPlayingIfAppropriate();
        }

        /// <summary>
        /// Play the item once the current item has finished
        /// </summary>
        /// <param name="i">the item to play next</param>
        public void PlayNext(PlayableItem i)
        {
            if (this.PlayQueue.Count <= 1 || this.PlayQueue[0] == i) // if the queue has one or less items, 'play next' is meaningless.  If the chosen item is already playing, there is nothing to do.
                return;

            this.PlayQueue.Remove(i); // Remove it from wherever it was in the queue
            this.PlayQueue.Insert(1, i); // and insert it below the top item
            this.StartPlayingIfAppropriate(); // and start playing, if the player was in a hard-stopped state.
        }

        /// <summary>
        /// Stop playing whatever we are playing, and start playing the chosen track immediately, if we were previously playing a track.
        /// </summary>
        /// <param name="i">the item to start playing</param>
        public void PlayNow(PlayableItem i)
        {
            if (this.PlayQueue.Count <= 1 || this.PlayQueue[0] == i) // if the queue has one or less items, 'play next' is meaningless.  If the chosen item is already playing, there is nothing to do.
                return;

            bool wasPlayingBefore = false;
            if (ToccataModel.MediaPlayerIsPlaying())
                wasPlayingBefore = true;

            this.Stop(); // stop playing the current item

            this.PlayQueue.Remove(i); // Remove the chosen item from wherever it was in the queue
            this.PlayQueue.Insert(0, i); // and put it at the top of the play queue

            if (wasPlayingBefore)
                this.StartPlayingIfAppropriate(); // and, if we were playing previously, start playing at the start of the track we just chose.
        }

        /// <summary>
        /// Loads a list of playable items (storage files) from a file, and puts them onto the bottom of the play queue.
        /// </summary>
        /// <param name="f">The file to read the items from</param>
        /// <returns></returns>
        public async Task<bool> LoadPlayQueue(StorageFile f)
        {
            try
            {
                using (Stream fr1 = await WindowsRuntimeStorageExtensions.OpenStreamForReadAsync((IStorageFile)f))
                {
                    using (TextReader tr1 = new StreamReader(fr1))
                    {
                        string path;
                        while ((path = tr1.ReadLine()) != null) // the file holds one file path per line
                        {
                            try
                            {
                                StorageFile trackFile =await  StorageFile.GetFileFromPathAsync(path);
                                this.PlayQueue.Add(new PlayableItem(trackFile));
                            }
                            catch (Exception )
                            {
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception )
            {
                return false;
            }
        }

        /// <summary>
        /// Sorts the play queue into a random order.
        /// </summary>
        public void ShufflePlayQueue()
        {
            bool flag = false;
            if (ToccataModel.MediaPlayerIsPlaying())
                flag = true;
            this.Stop();
            List<PlayableItem> playableItemList = new List<PlayableItem>((IEnumerable<PlayableItem>)this.PlayQueue);
            this.PlayQueue.Clear();
            Random random = new Random();
            while ((uint)playableItemList.Count > 0U)
            {
                int index = 0;
                if (playableItemList.Count > 1)
                    index = random.Next(0, playableItemList.Count);
                PlayableItem playableItem = playableItemList[index];
                playableItemList.RemoveAt(index);
                this.PlayQueue.Add(playableItem);
            }
            if (!flag)
                return;
            this.StartPlayingIfAppropriate();
        }

        /// <summary>
        /// Saves the current play queue to a file.
        /// </summary>
        /// <param name="f">the file to save to</param>
        /// <returns></returns>
        public async Task<bool> SavePlayQueue(StorageFile f)
        {
            try
            {
                List<string> paths = new List<string>();
                foreach (PlayableItem play in (Collection<PlayableItem>)this.PlayQueue)
                {
                    PlayableItem i = play;
                    paths.Add(i.storage.Path);
                }
                using (Stream fr1 = await WindowsRuntimeStorageExtensions.OpenStreamForWriteAsync((IStorageFile)f))
                {
                    using (StreamWriter tr = new StreamWriter(fr1))
                    {
                        foreach (string str in paths)
                        {
                            string s = str;
                            tr.WriteLine(s); // the file holds one file path per line
                        }
                    }
                }
                return true;
            }
            catch (Exception )
            {
                return false;
            }
        }


        private DateTime dtNextSliderUpdate = DateTime.MinValue; // when will it next be reasonable to update the UI (slider)
        /// <summary>
        /// Callled from time to time by the player, to update on its progress through the track.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="total"></param>
        public void OnPlaybackPositionChanged(TimeSpan current, TimeSpan total)
        {
            // The UI does not get update every time that the player reports some progress.  It gets updated:
            // At the end of the track (current == total)
            // At the start of the track (current = 0)
            // Otherwise, no more than once per second.
            if (DateTime.Now > dtNextSliderUpdate || current==total || current==TimeSpan.Zero)
            {
                dtNextSliderUpdate = DateTime.Now.AddSeconds(1);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                MainPage.StaticDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { MainPage.SetSliderPosition(current, total); });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        /// <summary>
        /// Called when playback state changes (for example, from 'playing' to 'paused' when end of track is reached).
        /// </summary>
        /// <param name="s">The state we are changing into</param>
        /// <param name="trackFinished">true if this looks like the end of a track (current==total)</param>
        public void OnPlaybackStateChanged(MediaPlaybackState s, bool trackFinished)
        {
            if (s == MediaPlaybackState.Paused)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                MainPage.StaticDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    MainPage.SetPlayButtonAppearance(false); // The Play button will display a "play" label, and do 'play' when tapped.

                    if (trackFinished) // paused, and at the end of a track
                    {
                        MainPage.SetNowPlaying(""); // set the text label at the bottom of the slider to blank.

                        if (PlayQueue.Count > 0) // (this is just defensive coding, it should always be true)
                            PlayQueue.RemoveAt(0); // We've finished this track, so remove it from the top of the play queue.

                        if (PlayQueue.Count > 0) // if there is now a track at the top of the queue ...
                            ToccataModel.Play(this.PlayQueue[0].storage); // ... start playing it.

                    }
                    else // paused but not finished the track
                    {

                    }
                });
            }
            else // playing, buffering, opening, none, or whatever
            {
                MainPage.StaticDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (s == MediaPlaybackState.Playing)
                    {
                        MainPage.SetPlayButtonAppearance(true); // The Play button will display a "pause" label, and do 'pause' when tapped.
                    }
                    else // could be 'buffering', 'opening' or 'none' - anyhow, it's a state in which the media is not yet playing.
                    {
                        MainPage.SetPlayButtonAppearance(false); // The Play button will display a "play" label, and do 'play' when tapped.
                    }

                    if (PlayQueue.Count > 0) // (this is just defensive coding, it should always be true)
                        MainPage.SetNowPlaying(PlayQueue[0].DisplayName + " (" + PlayQueue[0].storage.Path + ")");
                    else
                        MainPage.SetNowPlaying("");
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        private StorageFolder _RootFolder = null;
        /// <summary>
        /// The root folder, from which the app will read the list of artists.  It's hard-coded to the user's Music folder at present.
        /// When it's assigned, by side-effect, the Artists list will be loaded from it, and the lists of Albums and Tracks will be cleared.
        /// </summary>
        public StorageFolder RootFolder
        {
            get
            {
                return this._RootFolder;
            }
            set
            {
                if (value == this._RootFolder)
                    return;
                this._RootFolder = value;
                this.OnPropertyChanged();

                this.Artists.Clear();
                this.Albums.Clear();
                this.Tracks.Clear();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                ToccataModel.PopulateFolderItems(this.Artists, value);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        private double _PlayerPanelSize = 0.0;
        /// <summary>
        /// The size of the (square) panel in the UI that displays the player and the lists of artists, albums and tracks.  Cunning logic
        /// in MainPage.MainPage_SizeChanged assigns it to a value that works, and bindings in MainPage.xaml apply it to the relevant
        /// UI element.
        /// </summary>
        public double PlayerPanelSize
        {
            get
            {
                return this._PlayerPanelSize;
            }
            set
            {
                if (value == this._PlayerPanelSize)
                    return;
                this._PlayerPanelSize = value;
                this.OnPropertyChanged();
            }
        }

        private double _QueuePanelWidth = 0.0;
        /// <summary>
        /// The width of the (rectangular) panel in the UI that displays the play queue.  Cunning logic in MainPage.MainPage_SizeChanged
        /// assigns it to a value that works, and bindings in MainPage.xaml apply it to the relevant UI element.
        /// </summary>
        public double QueuePanelWidth
        {
            get
            {
                return this._QueuePanelWidth;
            }
            set
            {
                if (value == this._QueuePanelWidth)
                    return;
                this._QueuePanelWidth = value;
                this.OnPropertyChanged();
            }
        }

        private double _QueuePanelHeight = 0.0;
        /// <summary>
        /// The width of the (rectangular) panel in the UI that displays the play queue.  Cunning logic in MainPage.MainPage_SizeChanged
        /// assigns it to a value that works, and bindings in MainPage.xaml apply it to the relevant UI element.
        /// </summary>
        public double QueuePanelHeight
        {
            get
            {
                return this._QueuePanelHeight;
            }
            set
            {
                if (value == this._QueuePanelHeight)
                    return;
                this._QueuePanelHeight = value;
                this.OnPropertyChanged();
            }
        }

        public Orientation _PlayerOrientation = Orientation.Horizontal;
        /// <summary>
        /// Depending on whether the screen is landscape or portrait, the play queue panel is either displayed below the player panel
        /// or to the right of it.  This property (by being bound to a StackPanel's layout) controls which of these is done; 
        /// Cunning logic in MainPage.MainPage_SizeChanged assigns the property appropriately.
        /// </summary>
        public Orientation PlayerOrientation
        {
            get
            {
                return this._PlayerOrientation;
            }
            set
            {
                if (value == this._PlayerOrientation)
                    return;
                this._PlayerOrientation = value;
                this.OnPropertyChanged();
            }
        }

        private ObservableCollection<FolderEntry> _Artists = new ObservableCollection<FolderEntry>();
        /// <summary>
        /// The bindable collection of artist items.
        /// </summary>
        public ObservableCollection<FolderEntry> Artists
        {
            get
            {
                return this._Artists;
            }
        }

        private ObservableCollection<FolderEntry> _Albums = new ObservableCollection<FolderEntry>();
        /// <summary>
        /// The bindable collection of album items.
        /// </summary>
        public ObservableCollection<FolderEntry> Albums
        {
            get
            {
                return this._Albums;
            }
        }

        /// <summary>
        /// The bindable collection of track items.
        /// </summary>
        private ObservableCollection<FolderEntry> _Tracks = new ObservableCollection<FolderEntry>();
        public ObservableCollection<FolderEntry> Tracks
        {
            get
            {
                return this._Tracks;
            }
        }

        /// <summary>
        /// The bindable collection of items that are queued to play.
        /// </summary>
        private ObservableCollection<PlayableItem> _PlayQueue = new ObservableCollection<PlayableItem>();
        public ObservableCollection<PlayableItem> PlayQueue
        {
            get
            {
                return this._PlayQueue;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = (_param1, _param2) => { }; // UWP boilerplate

        public void OnPropertyChanged([CallerMemberName] string propertyName = null) // UWP boilerplate
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged == null)
                return;
            propertyChanged((object)this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
