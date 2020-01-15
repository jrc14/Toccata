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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Toccata.ViewModel;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Core;


namespace Toccata.Model
{
    /// <summary>
    /// ToccataModel provides a static member to hold a media player object, and various static methods to manipulate it, and to manage
    /// the lists of tracks and folders.
    /// Do not instantiate the class.
    /// </summary>
    public class ToccataModel
    {
        /// <summary>
        /// Reads a folder, and populates a collection of FolderEntries from it.  All folders are read in, plus any music files
        /// (WMA and  MP3 extensions).  The collection is sorted: folders in alphabetic order, then tracks sorted by track number
        /// (the track number is assumed to be the start of the file name; if the file names don't start with a number, then
        /// the method will look in the music metadata of the files).
        /// An optional string parameter will, if supplied, alter the method's behaviour, so it only adds items whose DisplayNames contain that string.
        /// In that case, you can also pass in a list, unfilteredItems, which will get a list of all items, whether or not they matched the filter.
        /// </summary>
        /// <param name="collection">collection to be populated</param>
        /// <param name="folder">the folder to be read</param>
        /// <param name="displayNameFilter">optional filter, to restrict items by DisplayName</param>
        /// <param name="unfilteredItems">optional list, in which the unfiltered list of items will be returned</param>
        /// <returns></returns>
        public static async Task<bool> PopulateFolderItems( ObservableCollection<FolderEntry> collection, StorageFolder folder, string displayNameFilter=null, List<FolderEntry> unfilteredItems=null)
        {
            bool success = true;

            List<StorageFile> files = new List<StorageFile>();

            try
            {
                foreach (StorageFile f in await folder.GetFilesAsync())
                {
                    if (f.FileType.ToUpper() == ".MP3" || f.FileType.ToUpper() == ".WMA")
                    {
                        files.Add(f);
                    }
                }
            }
            catch (Exception )
            {
                success = false;
            }

            if (!success)
                return false;

            bool allNamesStartWithNumbers = true;

            foreach (StorageFile f in files)
            {
                if (f.DisplayName == null || f.DisplayName.Length < 1)
                {
                    allNamesStartWithNumbers = false;
                    break;
                }
                if (f.DisplayName[0] < '0' || f.DisplayName[0] > '9')
                {
                    allNamesStartWithNumbers = false;
                    break;
                }
            }

            if (allNamesStartWithNumbers)
            {
                files.Sort((f1, f2) => f1.DisplayName.CompareTo(f2.DisplayName));
            }
            else
            {
                try
                {
                    Dictionary<string, uint> trackNumbers = new Dictionary<string, uint>();

                    foreach (StorageFile f in files)
                    {
                        MusicProperties p = await f.Properties.GetMusicPropertiesAsync();
                        trackNumbers[f.Path] = p.TrackNumber;
                    }

                    files.Sort((f1, f2) => trackNumbers[f1.Path].CompareTo(trackNumbers[f2.Path]));
                }
                catch (Exception )
                {
                    success = false;
                }
            }

            if (!success)
                return false;

            List<StorageFolder> folders = new List<StorageFolder>();

            try
            {
                foreach (StorageFolder f in await folder.GetFoldersAsync())
                {
                    folders.Add(f);
                }
            }
            catch (Exception)
            {
                success = false;
            }

            if (!success)
                return false;

            folders.Sort((f1, f2) => f1.DisplayName.CompareTo(f2.DisplayName));

            foreach (StorageFolder f in folders)
            {
                // The ugly expression is:
                //   true (==keep the file) if displayNameFilter is null or empty
                //   true (==keep the file) if displayNameFilter is contained in the entry's DisplayName (case-insensitive)
                //   false (==skip the file) otherwise, i.e. the displayNameFilter is specified but the entry does not match it.
                if (String.IsNullOrEmpty(displayNameFilter) || f.DisplayName.ToUpper().Contains(displayNameFilter.ToUpper()))
                {
                    collection.Add(new FolderEntry(f));
                }

                if (unfilteredItems != null)
                    unfilteredItems.Add(new FolderEntry(f));
            }

            foreach (StorageFile f in files)
            {
                if (String.IsNullOrEmpty(displayNameFilter) || f.DisplayName.ToUpper().Contains(displayNameFilter.ToUpper()))
                {
                    collection.Add(new FolderEntry(f));
                }

                if (unfilteredItems != null)
                    unfilteredItems.Add(new FolderEntry(f));
            }

            return true;
        }

        private static MediaPlayer mp = null;
        /// <summary>
        /// Call this method once and only once, during initialisation.  It sets up the media player, and adds a handler for when
        /// media is opened (because we have some more setup to do, after the first time that media is opened).
        /// </summary>
        public static void SetUpMediaPlayer()
        {
            ToccataModel.mp = new MediaPlayer();

            ToccataModel.mp.MediaOpened += Mp_MediaOpened;
        }

        /// <summary>
        /// Returns true if the media player hasn't been set up yet, or if it has a null source.  The app sets the source to null
        /// to indicate that it's put the player into a 'hard stopped' state.
        /// </summary>
        /// <returns></returns>
        public static bool MediaPlayerHasNoSource()
        {
            return ToccataModel.mp == null || ToccataModel.mp.Source == null;
        }

        /// <summary>
        /// Returns true if the media player has been set up and is playing something.
        /// </summary>
        /// <returns></returns>
        public static bool MediaPlayerIsPlaying()
        {
            return ToccataModel.mp != null && ToccataModel.mp.Source != null && (ToccataModel.mp.PlaybackSession != null && ToccataModel.mp.PlaybackSession.PlaybackState == MediaPlaybackState.Playing);
        }

        /// <summary>
        /// Sets the source to the provided StorageFile and tells the media player to play it.
        /// </summary>
        /// <param name="f">the file to play</param>
        public static void Play(StorageFile f)
        {
            if (ToccataModel.mp == null)
                return;
            ToccataModel.mp.Source=MediaSource.CreateFromStorageFile(f);
            ToccataModel.mp.Play();
        }

        /// <summary>
        /// Tells the media player to start playing its current source, if it has one.
        /// </summary>
        public static void Play()
        {
            if (ToccataModel.mp == null || ToccataModel.mp.Source == null)
                return;

            ToccataModel.mp.Play();
        }

        /// <summary>
        /// Pauses the media player.
        /// </summary>
        public static void Pause()
        {
            if (ToccataModel.mp == null)
                return;

            ToccataModel.mp.Pause();
        }

        /// <summary>
        /// Stops the media player, and puts it into the 'hard stopped' state.
        /// </summary>
        public static void Stop()
        {
            if (ToccataModel.mp == null)
                return;

            ToccataModel.mp.Pause();

            if (ToccataModel.mp.PlaybackSession != null)
                ToccataModel.mp.PlaybackSession.Position=new TimeSpan(0L);

            ToccataModel.mp.Source = null;
        }

        /// <summary>
        /// Moves the player's playback position.
        /// </summary>
        /// <param name="t">move the playback position to</param>
        public static void SetPlayerPosition(TimeSpan t)
        {
            if (ToccataModel.mp == null || ToccataModel.mp.PlaybackSession == null || t > ToccataModel.mp.PlaybackSession.NaturalDuration)
                return;

            ToccataModel.mp.PlaybackSession.Position=t;
        }

        private static bool PLAYBACK_SESSION_SET_UP = false; // to tell us whether we need to complete once-only session initialisation
        private static void Mp_MediaOpened(MediaPlayer sender, object args)
        {
            if (ToccataModel.PLAYBACK_SESSION_SET_UP || ToccataModel.mp.PlaybackSession == null)
                return; // If the method's already been called, and completed setup, do nothing.  If the session doesn't exist yet, do nothing.

            mp.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged; // handler to update the UI when playback state changes
            mp.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged; // handler to update the UI when playback position changes

            ToccataModel.PLAYBACK_SESSION_SET_UP = true;

        }

        private static void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            MainViewModel.Instance.OnPlaybackPositionChanged(sender.Position, sender.NaturalDuration); // tell the UI about the new position
        }

        private static void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            bool trackFinished = false; //at the end of a track?

            if (sender.Position!= TimeSpan.Zero && sender.Position == sender.NaturalDuration) // Position is not at the start of the track, and is equal to the duration of the track
                trackFinished = true;                                                         // means we are at the end of a track

            MainViewModel.Instance.OnPlaybackStateChanged(sender.PlaybackState, trackFinished); // tell the UI about the state of playback.
        }
    }
}
