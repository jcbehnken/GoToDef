using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;

namespace GoToDef
{
    /// <summary>
    /// The state of the alt key for a given view, which is kept up-to-date by a combination of the
    /// key processor and the mouse process
    /// </summary>
    internal sealed class AltKeyState
    {
        internal static AltKeyState GetStateForView(ITextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty(typeof(AltKeyState), () => new AltKeyState());
        }

        bool _enabled = false;

        internal bool Enabled
        {
            get
            {
                // Check and see if alt is down but we missed it somehow.
                bool altDown = (Keyboard.Modifiers & ModifierKeys.Alt) != 0 &&
                                (Keyboard.Modifiers & ModifierKeys.Shift) == 0;
                if (altDown != _enabled)
                    Enabled = altDown;

                return _enabled;
            }
            set
            {
                bool oldVal = _enabled;
                _enabled = value;
                if (oldVal != _enabled)
                {
                    var temp = AltKeyStateChanged;
                    if (temp != null)
                        temp(this, new EventArgs());
                }
            }
        }

        internal event EventHandler<EventArgs> AltKeyStateChanged;
    }
}
