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
        private DateTime dtLastSliderUpdate = DateTime.Now;
        private List<PlayableItem> history = new List<PlayableItem>();
        public StorageFolder _RootFolder = (StorageFolder)null;
        public double _PlayerPanelSize = 0.0;
        public double _QueuePanelWidth = 0.0;
        public double _QueuePanelHeight = 0.0;
        public Orientation _PlayerOrientation = (Orientation)0;
        private ObservableCollection<FolderEntry> _Artists = new ObservableCollection<FolderEntry>();
        private ObservableCollection<FolderEntry> _Albums = new ObservableCollection<FolderEntry>();
        private ObservableCollection<FolderEntry> _Tracks = new ObservableCollection<FolderEntry>();
        private ObservableCollection<PlayableItem> _PlayQueue = new ObservableCollection<PlayableItem>();

        public static MainViewModel Instance
        {
            get
            {
                return MainViewModel._Instance;
            }
        }

        public void Initialise()
        {
            this.RootFolder = KnownFolders.get_MusicLibrary();
            ToccataModel.SetUpMediaPlayer();
        }

        public async void OpenArtistFolder(FolderEntry f)
        {
            this.Tracks.Clear();
            int num = (int)await ToccataModel.PopulateFolderItems(this.Albums, f.storage as StorageFolder);
        }

        public async void OpenAlbumFolder(FolderEntry f)
        {
            int num = (int)await ToccataModel.PopulateFolderItems(this.Tracks, f.storage as StorageFolder);
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
            if (this.PlayQueue.Count <= 0 || !ToccataModel.MediaPlayerHasNoSource())
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
                                TaskAwaiter<StorageFile> awaiter = (TaskAwaiter<StorageFile>)WindowsRuntimeSystemExtensions.GetAwaiter<StorageFile>((IAsyncOperation<M0>)StorageFile.GetFileFromPathAsync(path));
                                if (!((TaskAwaiter<StorageFile>)ref awaiter).get_IsCompleted())
                                {
                                    int num;
                                    // ISSUE: reference to a compiler-generated field
                                    this.\u003C\u003E1__state = num = 1;
                                    TaskAwaiter<StorageFile> taskAwaiter = awaiter;
                                    // ISSUE: variable of a compiler-generated type
                                    MainViewModel.\u003CLoadPlayQueue\u003Ed__19 stateMachine = this;
                                    // ISSUE: reference to a compiler-generated field
                                    this.\u003C\u003Et__builder.AwaitUnsafeOnCompleted < TaskAwaiter<StorageFile>, MainViewModel.\u003CLoadPlayQueue\u003Ed__19 > (ref awaiter, ref stateMachine);
                                    return;
                                }
                                StorageFile trackFile = ((TaskAwaiter<StorageFile>)ref awaiter).GetResult();
                                this.PlayQueue.Add(new PlayableItem(trackFile));
                                trackFile = (StorageFile)null;
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
                    paths.Add(i.storage.get_Path());
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

        public void OnPlaybackPositionChanged(TimeSpan current, TimeSpan total)
        {
            // ISSUE: object of a compiler-generated type is created
            // ISSUE: variable of a compiler-generated type
            MainViewModel.\u003C\u003Ec__DisplayClass23_0 cDisplayClass230 = new MainViewModel.\u003C\u003Ec__DisplayClass23_0();
            // ISSUE: reference to a compiler-generated field
            cDisplayClass230.current = current;
            // ISSUE: reference to a compiler-generated field
            cDisplayClass230.total = total;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            if (!(this.dtLastSliderUpdate.AddSeconds(1.0) < DateTime.Now) && !(cDisplayClass230.current == TimeSpan.Zero) && !(cDisplayClass230.current == cDisplayClass230.total))
                return;
            this.dtLastSliderUpdate = DateTime.Now;
            // ISSUE: method pointer
            MainPage.StaticDispatcher.RunAsync((CoreDispatcherPriority)0, new DispatchedHandler((object)cDisplayClass230, __methodptr(\u003COnPlaybackPositionChanged\u003Eb__0)));
        }

        public void OnPlaybackStateChanged(MediaPlaybackState s, bool trackFinished)
        {
            // ISSUE: object of a compiler-generated type is created
            // ISSUE: variable of a compiler-generated type
            MainViewModel.\u003C\u003Ec__DisplayClass25_0 cDisplayClass250 = new MainViewModel.\u003C\u003Ec__DisplayClass25_0();
            // ISSUE: reference to a compiler-generated field
            cDisplayClass250.\u003C\u003E4__this = this;
            // ISSUE: reference to a compiler-generated field
            cDisplayClass250.trackFinished = trackFinished;
            if (s == 3)
            {
                // ISSUE: method pointer
                MainPage.StaticDispatcher.RunAsync((CoreDispatcherPriority)0, new DispatchedHandler((object)cDisplayClass250, __methodptr(\u003COnPlaybackStateChanged\u003Eb__0)));
            }
            else
            {
                // ISSUE: method pointer
                MainPage.StaticDispatcher.RunAsync((CoreDispatcherPriority)0, new DispatchedHandler((object)cDisplayClass250, __methodptr(\u003COnPlaybackStateChanged\u003Eb__1)));
            }
        }

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
                this.OnPropertyChanged(nameof(RootFolder));
                ToccataModel.PopulateFolderItems(this.Artists, value);
            }
        }

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
                this.OnPropertyChanged(nameof(PlayerPanelSize));
            }
        }

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
                this.OnPropertyChanged(nameof(QueuePanelWidth));
            }
        }

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
                this.OnPropertyChanged(nameof(QueuePanelHeight));
            }
        }

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
                this.OnPropertyChanged(nameof(PlayerOrientation));
            }
        }

        public ObservableCollection<FolderEntry> Artists
        {
            get
            {
                return this._Artists;
            }
        }

        public ObservableCollection<FolderEntry> Albums
        {
            get
            {
                return this._Albums;
            }
        }

        public ObservableCollection<FolderEntry> Tracks
        {
            get
            {
                return this._Tracks;
            }
        }

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
