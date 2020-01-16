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

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Toccata
{
    /// <summary>
    /// ToccataSlider is a customised slider control that exposes events allowing you to tell when a physical user manipulation of the control
    /// is happening.  Credit to: https://stackoverflow.com/questions/48833441/how-do-i-listen-to-uwp-xaml-slider-manipulation-start-end-events
    /// </summary>
    public class ToccataSlider : Slider
    {
        public event EventHandler SliderManipulationStarted;
        public event EventHandler SliderManipulationCompleted;
        public event EventHandler SliderManipulationMoved;
        private bool IsSliderBeingManpulated
        {
            get
            {
                return this.isContainerHeld || this.isThumbHeld;
            }
        }


        private bool isThumbHeld = false;
        private bool isContainerHeld = false;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var thumb = base.GetTemplateChild("HorizontalThumb") as Thumb;
            if (thumb == null)
            {
                thumb = base.GetTemplateChild("VerticalThumb") as Thumb;
            }
            if (thumb != null)
            {
                thumb.DragStarted += this.Thumb_DragStarted;
                thumb.DragCompleted += this.Thumb_DragCompleted;
                thumb.DragDelta += this.Thumb_DragDelta;
            }

            var sliderContainer = base.GetTemplateChild("SliderContainer") as Grid;
            if (sliderContainer != null)
            {
                sliderContainer.AddHandler(PointerPressedEvent,
                    new PointerEventHandler(this.SliderContainer_PointerPressed), true);
                sliderContainer.AddHandler(PointerReleasedEvent,
                    new PointerEventHandler(this.SliderContainer_PointerReleased), true);
                sliderContainer.AddHandler(PointerMovedEvent,
                    new PointerEventHandler(this.SliderContainer_PointerMoved), true);
            }
        }

        private void SliderContainer_PointerMoved(object sender,
            Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.InvokeMove();
        }

        private void SliderContainer_PointerReleased(object sender,
            Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.SetContainerHeld(false);
        }

        private void SliderContainer_PointerPressed(object sender,
            Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.SetContainerHeld(true);
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            this.InvokeMove();
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            this.SetThumbHeld(false);
        }

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            this.SetThumbHeld(true);
        }

        private void SetThumbHeld(bool held)
        {
            bool wasManipulated = this.IsSliderBeingManpulated;
            this.isThumbHeld = held;
            this.InvokeStateChange(wasManipulated);
        }

        private void SetContainerHeld(bool held)
        {
            bool wasManipulated = this.IsSliderBeingManpulated;
            this.isContainerHeld = held;
            this.InvokeStateChange(wasManipulated);
        }

        private void InvokeMove()
        {
            this.SliderManipulationMoved?.Invoke(this, EventArgs.Empty);
        }

        private void InvokeStateChange(bool wasBeingManipulated)
        {
            if (wasBeingManipulated != this.IsSliderBeingManpulated)
            {
                if (this.IsSliderBeingManpulated)
                {
                    this.SliderManipulationStarted?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    this.SliderManipulationCompleted?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
