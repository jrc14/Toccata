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
    public class MainViewModel : INotifyPropertyChanged
    {

        private static MainViewModel _Instance = new MainViewModel();
        public static MainViewModel Instance
        {
            get
            {
                return MainViewModel._Instance;
            }
        }

        public void Initialise()
        {
            this.RootFolder = KnownFolders.MusicLibrary;
            ToccataModel.SetUpMediaPlayer();
        }

        public async void OpenArtistFolder(FolderEntry f)
        {
            this.Albums.Clear();
            await ToccataModel.PopulateFolderItems(this.Albums, f.storage as StorageFolder);
        }

        public async void OpenAlbumFolder(FolderEntry f)
        {
            this.Tracks.Clear();
            await ToccataModel.PopulateFolderItems(this.Tracks, f.storage as StorageFolder);
            if (this.PlayQueue.Count != 0)
                return;
            this.AddTracks();
        }

        public void AddTrackFileToQueue(FolderEntry f)
        {
            if (f.IsFolder)
                return;
            this.PlayQueue.Add(new PlayableItem(f.storage as StorageFile));
        }

        public void Stop()
        {
            ToccataModel.Stop();
        }

        public void Play()
        {
            if (ToccataModel.MediaPlayerHasNoSource())
            {
                if (this.PlayQueue.Count <= 0)
                    return;
                ToccataModel.Play(this.PlayQueue[0].storage);
            }
            else
                ToccataModel.Play();
        }

        public void Pause()
        {
            ToccataModel.Pause();
        }

        private List<PlayableItem> history = new List<PlayableItem>();
        public void Back()
        {
            if (this.history.Count <= 0)
                return;
            bool flag = false;
            if (ToccataModel.MediaPlayerIsPlaying())
                flag = true;
            this.Stop();
            this.PlayQueue.Insert(0, this.history[0]);
            this.history.RemoveAt(0);
            if (flag)
                this.StartPlayingIfAppropriate();
        }

        public void Next()
        {
            if (this.PlayQueue.Count <= 1)
                return;
            bool flag = false;
            if (ToccataModel.MediaPlayerIsPlaying())
                flag = true;
            this.Stop();
            this.history.Insert(0, this.PlayQueue[0]);
            this.PlayQueue.RemoveAt(0);
            if (flag)
                this.StartPlayingIfAppropriate();
        }

        public void AddTracks()
        {
            foreach (FolderEntry track in (Collection<FolderEntry>)this.Tracks)
                this.AddTrackFileToQueue(track);
            this.StartPlayingIfAppropriate();
        }

        public void StartPlayingIfAppropriate()
        {
            if (this.PlayQueue.Count == 0 || !ToccataModel.MediaPlayerHasNoSource())
                return;
            ToccataModel.Play(this.PlayQueue[0].storage);
        }

        public void ClearQueue()
        {
            ToccataModel.Stop();
            this.PlayQueue.Clear();
        }

        public void SetPlayerPosition(TimeSpan t)
        {
            ToccataModel.SetPlayerPosition(t);
        }

        public void DeleteFromQueue(PlayableItem i)
        {
            if (this.PlayQueue.Count <= 0)
                return;
            bool flag = false;
            if (ToccataModel.MediaPlayerIsPlaying())
                flag = true;
            if (this.PlayQueue[0] == i)
                this.Stop();
            this.PlayQueue.Remove(i);
            if (flag)
                this.StartPlayingIfAppropriate();
        }

        public void PlayNext(PlayableItem i)
        {
            if (this.PlayQueue.Count <= 1 || this.PlayQueue[0] == i)
                return;
            this.PlayQueue.Remove(i);
            this.PlayQueue.Insert(1, i);
            this.StartPlayingIfAppropriate();
        }

        public void PlayNow(PlayableItem i)
        {
            if (this.PlayQueue.Count <= 1 || this.PlayQueue[0] == i)
                return;
            bool flag = false;
            if (ToccataModel.MediaPlayerIsPlaying())
                flag = true;
            this.Stop();
            this.PlayQueue.Remove(i);
            this.PlayQueue.Insert(0, i);
            if (flag)
                this.StartPlayingIfAppropriate();
        }

        public async Task<bool> LoadPlayQueue(StorageFile f)
        {
            try
            {
                using (Stream fr1 = await WindowsRuntimeStorageExtensions.OpenStreamForReadAsync((IStorageFile)f))
                {
                    using (TextReader tr1 = (TextReader)new StreamReader(fr1))
                    {
                        string path;
                        while ((path = tr1.ReadLine()) != null)
                        {
                            try
                            {
                                StorageFile trackFile =await  StorageFile.GetFileFromPathAsync(path);
                                this.PlayQueue.Add(new PlayableItem(trackFile));
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                        path = (string)null;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

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

        public async Task<bool> SavePlayQueue(StorageFile f)
        {
            try
            {
                List<string> paths = new List<string>();
                foreach (PlayableItem play in (Collection<PlayableItem>)this.PlayQueue)
                {
                    PlayableItem i = play;
                    paths.Add(i.storage.Path);
                    i = (PlayableItem)null;
                }
                using (Stream fr1 = await WindowsRuntimeStorageExtensions.OpenStreamForWriteAsync((IStorageFile)f))
                {
                    using (StreamWriter tr = new StreamWriter(fr1))
                    {
                        foreach (string str in paths)
                        {
                            string s = str;
                            ((TextWriter)tr).WriteLine(s);
                            s = (string)null;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        private DateTime dtNextSliderUpdate = DateTime.MinValue;
        public void OnPlaybackPositionChanged(TimeSpan current, TimeSpan total)
        {
            if (DateTime.Now > dtNextSliderUpdate || current==total || current==TimeSpan.Zero)
            {
                dtNextSliderUpdate = DateTime.Now.AddSeconds(1);

                MainPage.StaticDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { MainPage.SetSliderPosition(current, total); });
            }
        }

        public void OnPlaybackStateChanged(MediaPlaybackState s, bool trackFinished)
        {
            if (s == MediaPlaybackState.Paused)
            {
                MainPage.StaticDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    MainPage.SetPlayButtonAppearance(false); // "play"

                    if (trackFinished)
                    {
                        MainPage.SetNowPlaying("");

                        if (PlayQueue.Count > 0)
                            PlayQueue.RemoveAt(0);

                        if (PlayQueue.Count > 0)
                            ToccataModel.Play(this.PlayQueue[0].storage);

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
                        MainPage.SetPlayButtonAppearance(true); // "pause"
                    }
                    else
                    {
                        MainPage.SetPlayButtonAppearance(false); // "play"
                    }

                    if (PlayQueue.Count > 0)
                        MainPage.SetNowPlaying(PlayQueue[0].DisplayName + " (" + PlayQueue[0].storage.Path + ")");
                    else
                        MainPage.SetNowPlaying("");
                });
            }
        }

        private StorageFolder _RootFolder = (StorageFolder)null;
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
                ToccataModel.PopulateFolderItems(this.Artists, value);
            }
        }

        private double _PlayerPanelSize = 0.0;
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

        public Orientation _PlayerOrientation = (Orientation)0;
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
        public ObservableCollection<FolderEntry> Artists
        {
            get
            {
                return this._Artists;
            }
        }

        private ObservableCollection<FolderEntry> _Albums = new ObservableCollection<FolderEntry>();
        public ObservableCollection<FolderEntry> Albums
        {
            get
            {
                return this._Albums;
            }
        }

        private ObservableCollection<FolderEntry> _Tracks = new ObservableCollection<FolderEntry>();
        public ObservableCollection<FolderEntry> Tracks
        {
            get
            {
                return this._Tracks;
            }
        }

        private ObservableCollection<PlayableItem> _PlayQueue = new ObservableCollection<PlayableItem>();
        public ObservableCollection<PlayableItem> PlayQueue
        {
            get
            {
                return this._PlayQueue;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = (_param1, _param2) => { };

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged == null)
                return;
            propertyChanged((object)this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
