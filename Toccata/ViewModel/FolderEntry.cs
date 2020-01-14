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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;


namespace Toccata.ViewModel
{
    /// <summary>
    /// A FolderEntry represents one of the entries in the list Artists, or Albums, or Tracks.  These entries can be files or folders.
    /// </summary>
    public class FolderEntry : INotifyPropertyChanged
    {
        /// <summary>
        /// Make an entry from a folder
        /// </summary>
        /// <param name="f">the folder</param>
        public FolderEntry(StorageFolder f)
        {
            this.storage = (object)f;
            this.IsFolder = true;
        }

        /// <summary>
        /// Make an entry from a file
        /// </summary>
        /// <param name="f">the file</param>
        public FolderEntry(StorageFile f)
        {
            this.storage = (object)f;
            this.IsFolder = false;
        }


        private object _storage = null;

        /// <summary>
        /// The storage object in this entry (either a StorageFolder or a StorageFile).  We don't really expect it to be null.
        /// </summary>
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
                        this.DisplayName = (value as StorageFile).DisplayName;
                        break;
                    case StorageFolder _:
                        this.DisplayName = (value as StorageFolder).DisplayName;
                        break;
                    default:
                        this.DisplayName = "INVALID";
                        break;
                }
            }
        }

        private bool _IsFolder = false;

        /// <summary>
        /// True is this.storage is expected to be a folder, else false.
        /// </summary>
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
                this.OnPropertyChanged();
            }
        }

        private string _DisplayName = "";
        /// <summary>
        /// The name you should display in text labels and the like.
        /// </summary>
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


        public event PropertyChangedEventHandler PropertyChanged = (o, e) => { }; // UWP boilerplate

        public void OnPropertyChanged([CallerMemberName] string propertyName = null) // UWP boilerplate
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged == null)
                return;

            propertyChanged((object)this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
