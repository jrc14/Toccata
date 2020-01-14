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

        public static async Task<bool> PopulateFolderItems(
          ObservableCollection<FolderEntry> collection,
          StorageFolder folder)
        {
            // ISSUE: object of a compiler-generated type is created
            // ISSUE: variable of a compiler-generated type
            ToccataModel.\u003C\u003Ec__DisplayClass0_0 cDisplayClass00 = new ToccataModel.\u003C\u003Ec__DisplayClass0_0();
            // ISSUE: reference to a compiler-generated field
            cDisplayClass00.folder = folder;
            // ISSUE: reference to a compiler-generated field
            cDisplayClass00.collection = collection;
            // ISSUE: reference to a compiler-generated field
            cDisplayClass00.success = true;
            // ISSUE: reference to a compiler-generated field
            cDisplayClass00.files = new List<StorageFile>();
            // ISSUE: reference to a compiler-generated field
            cDisplayClass00.folders = new List<StorageFolder>();
            // ISSUE: reference to a compiler-generated method
            await Task.Run(new Func<Task>(cDisplayClass00.\u003CPopulateFolderItems\u003Eb__0));
            // ISSUE: reference to a compiler-generated field
            if (!cDisplayClass00.success)
                return false;
            bool allNamesStartWithNumbers = true;
            // ISSUE: reference to a compiler-generated field
            List<StorageFile>.Enumerator enumerator1 = cDisplayClass00.files.GetEnumerator();
            try
            {
                while (enumerator1.MoveNext())
                {
                    StorageFile f = enumerator1.Current;
                    if (f.get_DisplayName() == null || f.get_DisplayName().Length < 1)
                    {
                        allNamesStartWithNumbers = false;
                        break;
                    }
                    if (f.get_DisplayName()[0] < '0' || f.get_DisplayName()[0] > '9')
                    {
                        allNamesStartWithNumbers = false;
                        break;
                    }
                    f = (StorageFile)null;
                }
            }
            finally
            {
                enumerator1.Dispose();
            }
            enumerator1 = new List<StorageFile>.Enumerator();
            if (allNamesStartWithNumbers)
            {
                // ISSUE: reference to a compiler-generated field
                cDisplayClass00.files.Sort((Comparison<StorageFile>)((f1, f2) => f1.get_DisplayName().CompareTo(f2.get_DisplayName())));
            }
            else
            {
                try
                {
                    // ISSUE: object of a compiler-generated type is created
                    // ISSUE: variable of a compiler-generated type
                    ToccataModel.\u003C\u003Ec__DisplayClass0_1 cDisplayClass01 = new ToccataModel.\u003C\u003Ec__DisplayClass0_1();
                    // ISSUE: reference to a compiler-generated field
                    cDisplayClass01.trackNumbers = new Dictionary<string, uint>();
                    // ISSUE: reference to a compiler-generated field
                    List<StorageFile>.Enumerator enumerator2 = cDisplayClass00.files.GetEnumerator();
                    try
                    {
                        while (enumerator2.MoveNext())
                        {
                            StorageFile f = enumerator2.Current;
                            TaskAwaiter<MusicProperties> awaiter = (TaskAwaiter<MusicProperties>)WindowsRuntimeSystemExtensions.GetAwaiter<MusicProperties>((IAsyncOperation<M0>)f.get_Properties().GetMusicPropertiesAsync());
                            if (!((TaskAwaiter<MusicProperties>)ref awaiter).get_IsCompleted())
                            {
                                int num;
                                // ISSUE: reference to a compiler-generated field
                                this.\u003C\u003E1__state = num = 1;
                                TaskAwaiter<MusicProperties> taskAwaiter = awaiter;
                                // ISSUE: variable of a compiler-generated type
                                ToccataModel.\u003CPopulateFolderItems\u003Ed__0 stateMachine = this;
                                // ISSUE: reference to a compiler-generated field
                                this.\u003C\u003Et__builder.AwaitUnsafeOnCompleted < TaskAwaiter<MusicProperties>, ToccataModel.\u003CPopulateFolderItems\u003Ed__0 > (ref awaiter, ref stateMachine);
                                return;
                            }
                            MusicProperties p = ((TaskAwaiter<MusicProperties>)ref awaiter).GetResult();
                            // ISSUE: reference to a compiler-generated field
                            cDisplayClass01.trackNumbers[f.get_Path()] = p.get_TrackNumber();
                            p = (MusicProperties)null;
                            f = (StorageFile)null;
                        }
                    }
                    finally
                    {
                        enumerator2.Dispose();
                    }
                    enumerator2 = new List<StorageFile>.Enumerator();
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated method
                    cDisplayClass00.files.Sort(new Comparison<StorageFile>(cDisplayClass01.\u003CPopulateFolderItems\u003Eb__4));
                    cDisplayClass01 = (ToccataModel.\u003C\u003Ec__DisplayClass0_1) null;
                }
                catch (Exception ex)
                {
                }
            }
            // ISSUE: reference to a compiler-generated field
            cDisplayClass00.folders.Sort((Comparison<StorageFolder>)((f1, f2) => f1.get_DisplayName().CompareTo(f2.get_DisplayName())));
            // ISSUE: method pointer
            await MainPage.StaticDispatcher.RunAsync((CoreDispatcherPriority)0, new DispatchedHandler((object)cDisplayClass00, __methodptr(\u003CPopulateFolderItems\u003Eb__3)));
            // ISSUE: reference to a compiler-generated field
            return cDisplayClass00.success;
        }

        public static void SetUpMediaPlayer()
        {
            ToccataModel.mp = new MediaPlayer();
            MediaPlayer mp = ToccataModel.mp;
            // ISSUE: method pointer
            WindowsRuntimeMarshal.AddEventHandler<TypedEventHandler<MediaPlayer, object>>(new Func<TypedEventHandler<MediaPlayer, object>, EventRegistrationToken>(mp.add_MediaOpened), new Action<EventRegistrationToken>(mp.remove_MediaOpened), new TypedEventHandler<MediaPlayer, object>((object)null, __methodptr(Mp_MediaOpened)));
        }

        public static bool MediaPlayerHasNoSource()
        {
            return ToccataModel.mp == null || ToccataModel.mp.get_Source() == null;
        }

        public static bool MediaPlayerIsPlaying()
        {
            return ToccataModel.mp != null && ToccataModel.mp.get_Source() != null && (ToccataModel.mp.get_PlaybackSession() != null && ToccataModel.mp.get_PlaybackSession().get_PlaybackState() == 3);
        }

        public static void Play(StorageFile f)
        {
            if (ToccataModel.mp == null)
                return;
            ToccataModel.mp.put_Source((IMediaPlaybackSource)MediaSource.CreateFromStorageFile((IStorageFile)f));
            ToccataModel.mp.Play();
        }

        public static void Play()
        {
            if (ToccataModel.mp == null || ToccataModel.mp.get_Source() == null)
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
            if (ToccataModel.mp.get_PlaybackSession() != null)
                ToccataModel.mp.get_PlaybackSession().put_Position(new TimeSpan(0L));
            ToccataModel.mp.put_Source((IMediaPlaybackSource)null);
        }

        public static void SetPlayerPosition(TimeSpan t)
        {
            if (ToccataModel.mp == null || (ToccataModel.mp.get_PlaybackSession() == null || !(t < ToccataModel.mp.get_PlaybackSession().get_NaturalDuration())))
                return;
            ToccataModel.mp.get_PlaybackSession().put_Position(t);
        }

        private static void Mp_MediaOpened(MediaPlayer sender, object args)
        {
            if (ToccataModel.PLAYBACK_SESSION_SET_UP || ToccataModel.mp.get_PlaybackSession() == null)
                return;
            ToccataModel.PLAYBACK_SESSION_SET_UP = true;
            MediaPlaybackSession playbackSession1 = ToccataModel.mp.get_PlaybackSession();
            // ISSUE: method pointer
            WindowsRuntimeMarshal.AddEventHandler<TypedEventHandler<MediaPlaybackSession, object>>(new Func<TypedEventHandler<MediaPlaybackSession, object>, EventRegistrationToken>(playbackSession1.add_PlaybackStateChanged), new Action<EventRegistrationToken>(playbackSession1.remove_PlaybackStateChanged), new TypedEventHandler<MediaPlaybackSession, object>((object)null, __methodptr(PlaybackSession_PlaybackStateChanged)));
            MediaPlaybackSession playbackSession2 = ToccataModel.mp.get_PlaybackSession();
            // ISSUE: method pointer
            WindowsRuntimeMarshal.AddEventHandler<TypedEventHandler<MediaPlaybackSession, object>>(new Func<TypedEventHandler<MediaPlaybackSession, object>, EventRegistrationToken>(playbackSession2.add_PositionChanged), new Action<EventRegistrationToken>(playbackSession2.remove_PositionChanged), new TypedEventHandler<MediaPlaybackSession, object>((object)null, __methodptr(PlaybackSession_PositionChanged)));
        }

        private static void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            MainViewModel.Instance.OnPlaybackPositionChanged(sender.get_Position(), sender.get_NaturalDuration());
        }

        private static void PlaybackSession_PlaybackStateChanged(
          MediaPlaybackSession sender,
          object args)
        {
            bool trackFinished = false;
            if (sender.get_Position() != TimeSpan.Zero && sender.get_Position() == sender.get_NaturalDuration())
                trackFinished = true;
            MainViewModel.Instance.OnPlaybackStateChanged(sender.get_PlaybackState(), trackFinished);
        }
    }
}
