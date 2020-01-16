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
using Windows.UI.Xaml;

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
        /// It sets up and reads the root folder (where the list of artists is).  This is initialised to the user's top-level Music
        ///  folder, but can be changed.  Also it sets up the media player.
        /// </summary>
        public void Initialise()
        {
            this.RootFolder = KnownFolders.MusicLibrary; // a side-effect in the accessor will cause the folder to be read in to the Artists list.
            ToccataModel.SetUpMediaPlayer();
        }

        /// <summary>
        /// Clears the existing lists of albums and tracks, and then loads a new list of albums for a given artist.
        /// </summary>
        /// <param name="f">the folder whose name is the artist name</param>
        public async void OpenArtistFolder(FolderEntry f)
        {
            this.Albums.Clear();
            this.Tracks.Clear();

            if (string.IsNullOrEmpty(f.DisplayName))
                this.AlbumsFolderLabel = "";
            else
                this.AlbumsFolderLabel = "(in " + f.DisplayName + ")";

            this.TracksFolderLabel = "";

            await ToccataModel.PopulateFolderItems(this.Albums, f.storage as StorageFolder);
        }

        /// <summary>
        /// Clears the existing lists of tracks, and then loads a new list of tracks for a given album.
        /// </summary>
        /// <param name="f">the folder whose name is the album name</param>
        public async void OpenAlbumFolder(FolderEntry f)
        {
            this.Tracks.Clear();

            if (string.IsNullOrEmpty(f.DisplayName))
                this.TracksFolderLabel = "";
            else
                this.TracksFolderLabel = "(in " + f.DisplayName + ")";

            await ToccataModel.PopulateFolderItems(this.Tracks, f.storage as StorageFolder);

            if (this.PlayQueue.Count == 0) // if there are no tracks already queued to play, 
                this.AddTracks(); // auto-add the ones we just found in this folderto the play queue 
        }

        /// <summary>
        /// Adds a track to the bottom of the play queue.
        /// </summary>
        /// <param name="f">the track file to add</param>
        public void AddTrackFileToQueue(FolderEntry f)
        {
            if (f.IsFolder)
                return;

            this.PlayQueue.Add(new PlayableItem(f.storage as StorageFile));
        }

        /// <summary>
        /// Stops the media player (it puts it into the 'hard-stopped' state).
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
        /// Pauses playback (which is to say, stops it, but does not hard-stop it).
        /// </summary>
        public void Pause()
        {
            ToccataModel.Pause();
        }

        /// <summary>
        ///  A list of the tracks we have finished (so the back button can go back to them).  Tracks we have skipped using the 
        ///  'Next' function will also  be added to it.
        /// </summary>
        private List<PlayableItem> history = new List<PlayableItem>();

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
        /// Stop playing the current track and move it to the history, then start playing the track that is now the top of the play queue.  This is the right
        /// metyhod to call when the 'Next' button is tapped.
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
        /// <param name="t">playback position to set</param>
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
                this.Stop(); // then hard-stop playback

            this.PlayQueue.Remove(i);

            if (wasPlayingBefore)
                this.StartPlayingIfAppropriate();
        }

        /// <summary>
        /// Play the chosen track once the current one has finished.  This is the right method to call when the 'Play Next' context
        /// menu item is tapped.
        /// </summary>
        /// <param name="i">the item to play next</param>
        public void PlayNext(PlayableItem i)
        {
            if (this.PlayQueue.Count <= 1 || this.PlayQueue[0] == i) // if the queue has one or less items, 'play next' is meaningless.  If the chosen item is already playing, there is nothing to do.
                return;

            this.PlayQueue.Remove(i); // Remove it from wherever it was in the queue
            this.PlayQueue.Insert(1, i); // and insert it below the top item.
        }

        /// <summary>
        /// Stop playing whatever we are playing, and start playing the chosen track immediately.  This is trhe right method to call
        /// when the 'Play Now' context menu item is tapped.
        /// </summary>
        /// <param name="i">the item to start playing</param>
        public void PlayNow(PlayableItem i)
        {
            if (this.PlayQueue.Count <= 1 || this.PlayQueue[0] == i) // if the queue has one or less items, 'play next' is meaningless.  If the chosen item is already playing, there is nothing to do.
                return;

            this.Stop(); // stop playing the current item

            this.PlayQueue.Remove(i); // Remove the chosen item from wherever it was in the queue
            this.PlayQueue.Insert(0, i); // and put it at the top of the play queue.

            this.StartPlayingIfAppropriate(); // and start playing.
        }

        /// <summary>
        /// Sorts the play queue into a random order.
        /// </summary>
        public void ShufflePlayQueue()
        {
            bool wasPlayingBefore = false;
            if (ToccataModel.MediaPlayerIsPlaying())
                wasPlayingBefore = true;

            this.Stop();

            // Construct a list of all the items in the play queue (we will pull random items off it, in the code below).
            List<PlayableItem> playableItemList = new List<PlayableItem>((IEnumerable<PlayableItem>)this.PlayQueue);

            this.PlayQueue.Clear();

            Random random = new Random();
            while ((uint)playableItemList.Count > 0U)
            {
                int index = 0;
                if (playableItemList.Count > 1)
                    index = random.Next(0, playableItemList.Count); // pick a random index in the list

                PlayableItem playableItem = playableItemList[index]; // add the random item to the play queue, and remove it from the list. 
                playableItemList.RemoveAt(index);
                this.PlayQueue.Add(playableItem);
            }

            if (wasPlayingBefore)
                this.StartPlayingIfAppropriate();
        }

        /// <summary>
        /// Loads a list of playable items (storage files) from a file, and puts them onto the bottom of the play queue.
        /// </summary>
        /// <param name="f">The file to read the items from</param>
        /// <returns>true if the operation succeeded</returns>
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
                                StorageFile trackFile = await StorageFile.GetFileFromPathAsync(path);
                                this.PlayQueue.Add(new PlayableItem(trackFile));
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Saves the current play queue to a file.
        /// </summary>
        /// <param name="f">the file to save to</param>
        /// <returns>true if it managed to save OK</returns>
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

        private DateTime dtNextSliderUpdate = DateTime.MinValue; // When will it next be reasonable to update the UI (slider)?
        /// <summary>
        /// Callled from time to time by the player, to give the viewmodel an update on its progress through the track.
        /// </summary>
        /// <param name="current">where we have got to in the track</param>
        /// <param name="total">the length of the track</param>
        public void OnPlaybackPositionChanged(TimeSpan current, TimeSpan total)
        {
            // The UI does not get updated every time that the player reports some progress.  It gets updated:
            // 1) At the end of the track (current == total)
            // 2) At the start of the track (current = 0)
            // 3) Otherwise, no more than once per second.
            if (DateTime.Now > dtNextSliderUpdate || current==total || current==TimeSpan.Zero)
            {
                dtNextSliderUpdate = DateTime.Now.AddSeconds(1);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                MainPage.StaticDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { MainPage.SetSliderPosition(current, total); });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        /// <summary>
        /// Called by the player to let the viewmodel know when playback state changes (for example, from 'playing' to 'paused'
        /// when end of track is reached).  This method adjusts the appearance of the Pause/Play button as needed,
        /// and kicks off playback of the next track in the queue, if there is one.
        /// </summary>
        /// <param name="s">the state we are changing into</param>
        /// <param name="trackFinished">true if this looks like the end of a track (non-zero current==total)</param>
        public void OnPlaybackStateChanged(MediaPlaybackState s, bool trackFinished)
        {
            if (s == MediaPlaybackState.Paused)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                MainPage.StaticDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    MainPage.SetPlayButtonAppearance(false); // Set the Play button to display a "play" label, and do 'play' when tapped.

                    if (trackFinished) // paused, and at the end of a track
                    {
                        MainPage.SetNowPlaying(""); // set the text label at the bottom of the slider to blank.

                        if (PlayQueue.Count > 0) // (this is just defensive coding, it should always be true)
                            PlayQueue.RemoveAt(0); // We've finished this track, so remove it from the top of the play queue.

                        if (PlayQueue.Count > 0) // if there is now a track at the top of the queue ...
                            ToccataModel.Play(this.PlayQueue[0].storage); // ... start playing it.
                        else
                            ToccataModel.Stop(); // ... otherwise, we should hard-stop the player, leaving it ready to play something, when something is queued up.
                    }
                    else // paused but not finished the track - e.g. because the user tapped the Pause button.
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
                        MainPage.SetPlayButtonAppearance(true); // Set the Play button to display a "pause" label, and do 'pause' when tapped.
                    }
                    else // could be 'buffering', 'opening' or 'none' - anyhow, it's a state in which the media is not (yet) playing.
                    {
                        MainPage.SetPlayButtonAppearance(false); // Set the Play button to display a "play" label, and do 'play' when tapped.
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
        /// The root folder, from which the app will read the list of artists.  It gets initialised to the user's Music folder,
        /// but can be changed via the UI.  Assigning a location anywhere other than 'somewhere under the user's Music folder' will
        /// cause app permissioning problems, however.
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

                if(string.IsNullOrEmpty(value.DisplayName))
                    this.ArtistsFolderLabel = "";
                else
                    this.ArtistsFolderLabel = "(in "+value.DisplayName+")";

                this.AlbumsFolderLabel = "";
                this.TracksFolderLabel = "";

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                ToccataModel.PopulateFolderItems(this.Artists, value, this.ArtistNameFilter, this._RootFolderUnfilteredContents);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        /// <summary>
        /// Used to keep a list of the unfiltered contents of the Artist list (when we populate the Artist list itself, we normally
        /// filter it according to the filter string in this.ArtistNameFilter).  It's helpful when the filter string is changed,
        /// because sometimes we then need to refer back to contents of folder to restore the full list of artists, but we want to be
        /// able to do that without rescanning the folder, as this is a slow operation.
        /// </summary>
        private List<FolderEntry> _RootFolderUnfilteredContents = new List<FolderEntry>();

        private string _ArtistNameFilter = "";
        /// <summary>
        /// If this string is supplied (not null or empty) then it will act to filter the list of artist names - when this.Artists gets
        /// populated, items having DisplayName not containing the filter text will be skipped.
        /// When ArtistNameFilter is changed, the set accessor will update this.Artists, enforcing this filtering logic.
        /// WARNING: Because any change to this member variable means a scan through a potentially long list of artists,
        /// with a bunch of updates that will affect bound UI elements, it's a bad idea to change the variable too frequently.
        /// Consider throttling any code that does this.
        /// 
        /// To encourage you to do so, I provide a method SetArtistNameFilter that sets the filter in a duly throttled way.
        /// </summary>
        public string ArtistNameFilter
        {
            get
            {
                return this._ArtistNameFilter;
            }
            private set
            {
                if (value == this._ArtistNameFilter)
                    return;

                // If the previous filter value was null, or if the old filter value is a substring of the new one, we just need to
                // prune the collection in this.Artists.
                if (String.IsNullOrEmpty(this._ArtistNameFilter) || value.Contains(this._ArtistNameFilter))
                {
                    // First build a list of items to remove, then remove them seriatim.  We don't just iterate down the collection
                    // removing things as we go, because that will get us a 'collection modified' exception from the iterator.
                    List<FolderEntry> toRemove = new List<FolderEntry>();
                    string v = value.ToUpper();
                    foreach (FolderEntry f in this.Artists)
                    {
                        if (!f.DisplayName.ToUpper().Contains(v))
                            toRemove.Add(f);
                    }

                    foreach (FolderEntry f in toRemove)
                    {
                        this.Artists.Remove(f);
                    }
                }
                else // else, we need to refer to the the cached contents of artists directory, and populate the Artists collection according to the new filter string.
                {
                    this.Artists.Clear();

                    foreach (FolderEntry f in this._RootFolderUnfilteredContents)
                    {
                        if (f.DisplayName.ToUpper().Contains(value.ToUpper()))
                            this.Artists.Add(f);
                    }
                }

                this._ArtistNameFilter = value;

                this.OnPropertyChanged();
            }
        }

        private DispatcherTimer timer_SetArtistNameFilter=null;
        private DateTime lastApplied_SetArtistNameFilter = DateTime.MinValue;
        private string valueToApply_SetArtistNameFilter = null;
        /// <summary>
        /// Applies a new artist name filter string, taking care not to do so more often than every 500ms (if it is called more frequently
        /// than that, it will throw away some invocations, but make sure it always applies the most recently supplied string).
        /// </summary>
        /// <param name="f">the filter string to apply</param>
        public void SetArtistNameFilter(string f)
        {
            if(!MainPage.StaticDispatcher.HasThreadAccess) // If we aren't on the UI thread, put the task onto the UI thread, and move on.
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                MainPage.StaticDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { SetArtistNameFilter(f); });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                return;
            }
            
            DateTime dtNow = DateTime.Now;

            if(lastApplied_SetArtistNameFilter.AddMilliseconds(500)<dtNow) // the last value was applied more than 500ms ago, so it's OK to just apply a new value
            {
               lastApplied_SetArtistNameFilter = dtNow;

                this.ArtistNameFilter = f;
                
                return;
            }
            else // a value has been applied in the last 500ms - don't apply a new one
            {
                if (timer_SetArtistNameFilter != null) // there is a timer running; when it finishes it will apply whatever value is in valueToApply_SetArtistNameFilter
                {
                    valueToApply_SetArtistNameFilter = f;

                    return;
                }
                else // no timer is running - start one, which will apply, in 500ms, whatever is then the latest value we have been given
                {
                   valueToApply_SetArtistNameFilter = f;

                    timer_SetArtistNameFilter = new DispatcherTimer();
                    timer_SetArtistNameFilter.Interval = TimeSpan.FromMilliseconds(500);
                    timer_SetArtistNameFilter.Tick += (object s, object e) =>
                    {
                        ((DispatcherTimer) s).Stop(); // a timer only ever gets to tick once.

                        if(s== timer_SetArtistNameFilter) // should always be true, except in nasty race conditions
                        {
                            lastApplied_SetArtistNameFilter = DateTime.Now; // update the 'time we last applied a value'
                            timer_SetArtistNameFilter = null; // there is no timer running any more                            
                            this.ArtistNameFilter = valueToApply_SetArtistNameFilter; // apply the most recently supplied value
                        }
                        else // if we have somehow managed to start two timers, ignore whichever one is no longer assigned to this.timer_SetArtistNameFilter
                        {

                        }
                    };

                    timer_SetArtistNameFilter.Start();

                    return;
                }
            }
        }

        private string _ArtistsFolderLabel;
        /// <summary>
        /// The human readable label for the folder holding the list of artists.  It is not worked out automatically; code should
        /// update it whenever it loads the artist list from some folder.
        /// </summary>
        public string ArtistsFolderLabel
        {
            get
            {
                return this._ArtistsFolderLabel;
            }
            private set
            {
                if (value == this._ArtistsFolderLabel)
                    return;
                this._ArtistsFolderLabel = value;

                this.OnPropertyChanged();
            }
        }

        private string _AlbumsFolderLabel;
        /// <summary>
        /// The human readable label for the folder holding the list of albumns.  It is not worked out automatically; code should
        /// update it whenever it loads the albums list from some folder.
        /// </summary>
        public string AlbumsFolderLabel
        {
            get
            {
                return this._AlbumsFolderLabel;
            }
            private set
            {
                if (value == this._AlbumsFolderLabel)
                    return;
                this._AlbumsFolderLabel = value;

                this.OnPropertyChanged();
            }
        }

        private string _TracksFolderLabel;
        /// <summary>
        /// The human readable label for the folder holding the list of tracks.  It is not worked out automatically; code should
        /// update it whenever it loads the tracks list from some folder.
        /// </summary>
        public string TracksFolderLabel
        {
            get
            {
                return this._TracksFolderLabel;
            }
            private set
            {
                if (value == this._TracksFolderLabel)
                    return;
                this._TracksFolderLabel = value;

                this.OnPropertyChanged();
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
        /// or to the right of it.  This property (by being bound to a StackPanel's orientation attribute) controls which of these is done; 
        /// cunning logic in MainPage.MainPage_SizeChanged assigns the property appropriately.
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
