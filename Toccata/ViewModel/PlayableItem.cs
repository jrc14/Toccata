using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;


namespace Toccata.ViewModel
{
    public class PlayableItem : INotifyPropertyChanged
    {
        public string _DisplayName = "";
        private StorageFile _storage = (StorageFile)null;

        public PlayableItem(StorageFile f)
        {
            this.storage = f;
        }

        public string DisplayName
        {
            get
            {
                return this._DisplayName;
            }
            private set
            {
                if (value == this._DisplayName)
                    return;
                this._DisplayName = value;
                this.OnPropertyChanged(nameof(DisplayName));
            }
        }

        public PlayableItem Self
        {
            get
            {
                return this;
            }
        }

        public StorageFile storage
        {
            get
            {
                return this._storage;
            }
            set
            {
                this._storage = value;
                if (value == null)
                    this.DisplayName = "NULL";
                else
                    this.DisplayName = value.get_DisplayName();
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
