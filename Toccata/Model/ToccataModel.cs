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
    public class ToccataModel
    {
        private static MediaPlayer mp = (MediaPlayer)null;
        private static bool PLAYBACK_SESSION_SET_UP = false;

        public static async Task<bool> PopulateFolderItems( ObservableCollection<FolderEntry> collection, StorageFolder folder)
        {
            bool success = true;

            List<StorageFile> files = new List<StorageFile>();

            try
            {
                foreach (StorageFile f in await folder.GetFilesAsync())
                    if (f.FileType.ToUpper() == ".MP3" || f.FileType.ToUpper() == ".WMA")
                        files.Add(f);
            }
            catch
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
                catch (Exception ex)
                {
                    success = false;
                }
            }

            if (!success)
                return false;

            List<StorageFolder> folders = null;

            try
            {
                folders = new List<StorageFolder>(await folder.GetFoldersAsync());
            }
            catch
            {
                success = false;
            }
            if (!success)
                return false;

            folders.Sort((f1, f2) => f1.DisplayName.CompareTo(f2.DisplayName));

            foreach (StorageFolder f in folders)
                collection.Add(new FolderEntry(f));

            foreach (StorageFile f in files)
                collection.Add(new FolderEntry(f));

            return true;
        }

        public static void SetUpMediaPlayer()
        {
            ToccataModel.mp = new MediaPlayer();
            MediaPlayer mp = ToccataModel.mp;

            mp.MediaOpened += Mp_MediaOpened;
        }

        public static bool MediaPlayerHasNoSource()
        {
            return ToccataModel.mp == null || ToccataModel.mp.Source == null;
        }

        public static bool MediaPlayerIsPlaying()
        {
            return ToccataModel.mp != null && ToccataModel.mp.Source != null && (ToccataModel.mp.PlaybackSession != null && ToccataModel.mp.PlaybackSession.PlaybackState == (MediaPlaybackState)3);
        }

        public static void Play(StorageFile f)
        {
            if (ToccataModel.mp == null)
                return;
            ToccataModel.mp.Source=MediaSource.CreateFromStorageFile(f);
            ToccataModel.mp.Play();
        }

        public static void Play()
        {
            if (ToccataModel.mp == null || ToccataModel.mp.Source == null)
                return;
            ToccataModel.mp.Play();
        }

        public static void Pause()
        {
            if (ToccataModel.mp == null)
                return;
            ToccataModel.mp.Pause();
        }

        public static void Stop()
        {
            if (ToccataModel.mp == null)
                return;

            ToccataModel.mp.Pause();

            if (ToccataModel.mp.PlaybackSession != null)
                ToccataModel.mp.PlaybackSession.Position=new TimeSpan(0L);

            ToccataModel.mp.Source = null;
        }

        public static void SetPlayerPosition(TimeSpan t)
        {
            if (ToccataModel.mp == null || ToccataModel.mp.PlaybackSession == null || t > ToccataModel.mp.PlaybackSession.NaturalDuration)
                return;

            ToccataModel.mp.PlaybackSession.Position=t;
        }

        private static void Mp_MediaOpened(MediaPlayer sender, object args)
        {
            if (ToccataModel.PLAYBACK_SESSION_SET_UP || ToccataModel.mp.PlaybackSession == null)
                return;

            mp.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            mp.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

            ToccataModel.PLAYBACK_SESSION_SET_UP = true;

        }

        private static void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            MainViewModel.Instance.OnPlaybackPositionChanged(sender.Position, sender.NaturalDuration);
        }

        private static void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            bool trackFinished = false;

            if (sender.Position!= TimeSpan.Zero && sender.Position == sender.NaturalDuration)
                trackFinished = true;

            MainViewModel.Instance.OnPlaybackStateChanged(sender.PlaybackState, trackFinished);
        }
    }
}
