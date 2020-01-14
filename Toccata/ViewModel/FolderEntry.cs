﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace Toccata.ViewModel
{
    public class FolderEntry : INotifyPropertyChanged
    {
        private object _storage = (object)null;
        public bool _IsFolder = false;
        public string _DisplayName = "";

        public FolderEntry(StorageFolder f)
        {
            this.storage = (object)f;
            this.IsFolder = true;
        }

        public FolderEntry(StorageFile f)
        {
            this.storage = (object)f;
            this.IsFolder = false;
        }

        public object storage
        {
            get
            {
                return this._storage;
            }
            private set
            {
                this._storage = value;
                switch (value)
                {
                    case null:
                        this.DisplayName = "NULL";
                        break;
                    case StorageFile _:
                        this.DisplayName = (value as StorageFile).get_DisplayName();
                        break;
                    case StorageFolder _:
                        this.DisplayName = (value as StorageFolder).get_DisplayName();
                        break;
                    default:
                        this.DisplayName = "INVALID";
                        break;
                }
            }
        }

        public bool IsFolder
        {
            get
            {
                return this._IsFolder;
            }
            private set
            {
                if (value == this._IsFolder)
                    return;
                this._IsFolder = value;
                this.OnPropertyChanged(nameof(IsFolder));
            }
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
}
