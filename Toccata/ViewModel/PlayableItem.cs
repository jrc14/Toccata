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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;


namespace Toccata.ViewModel
{
    /// <summary>
    /// Represents something that can be on the play queue - i.e. a StorageFile containing a music track.
    /// </summary>
    public class PlayableItem : INotifyPropertyChanged
    {
        /// <summary>
        /// Construct a playable item
        /// </summary>
        /// <param name="f">the file containing the music track</param>
        public PlayableItem(StorageFile f)
        {
            this.storage = f;
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
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Provided so I can bind a tag to it in MainPage.xaml
        /// </summary>
        public PlayableItem Self
        {
            get
            {
                return this;
            }
        }

        private StorageFile _storage = null;
        /// <summary>
        /// The StorageFile containing the music
        /// </summary>
        public StorageFile storage
        {
            get
            {
                return this._storage;
            }
            private set
            {
                this._storage = value;
                if (value == null)
                    this.DisplayName = "NULL";
                else
                    this.DisplayName = value.DisplayName;
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
