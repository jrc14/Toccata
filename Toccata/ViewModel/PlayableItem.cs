using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;


namespace Toccata.ViewModel
{
    public class PlayableItem : INotifyPropertyChanged
    {
        public PlayableItem(StorageFile f)
        {
            this.storage = f;
        }

        private string _DisplayName = "";
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
                this.OnPropertyChanged();
            }
        }

        public PlayableItem Self
        {
            get
            {
                return this;
            }
        }

        private StorageFile _storage = null;
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
                    this.DisplayName = value.DisplayName;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = (o, e) => { };

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged == null)
                return;
            propertyChanged((object)this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
