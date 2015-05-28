using System;
using System.ComponentModel.Composition;
using System.Windows.Input;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio;

namespace GoToDef
{
    [Export(typeof(IKeyProcessorProvider))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType("code")]
    [Name("AltPeek")]
    [Order(Before = "GoToDefKeyProcessorProvider")]
    internal sealed class AltPeekKeyProcessorProvider : IKeyProcessorProvider
    {
        public KeyProcessor GetAssociatedProcessor(IWpfTextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty(typeof(AltPeekKeyProcessor),
                                                                () => new AltPeekKeyProcessor(AltKeyState.GetStateForView(view)));
        }
    }

    /// <summary>
    /// The state of the alt key for a given view, which is kept up-to-date by a combination of the
    /// key processor and the mouse process
    /// </summary>
    internal sealed class AltKeyState : IKeyState
    {
        internal static AltKeyState GetStateForView(ITextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty(typeof(AltKeyState), () => new AltKeyState());
        }

        bool _enabled = false;

        public bool Enabled
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
                    var temp = KeyStateChanged;
                    if (temp != null)
                        temp(this, new EventArgs());
                }
            }
        }

        public event EventHandler<EventArgs> KeyStateChanged;
    }

    /// <summary>
    /// Listen for the alt key being pressed or released to update the AltKeyStateChanged for a view.
    /// </summary>
    internal sealed class AltPeekKeyProcessor : KeyProcessorBase
    {
        AltKeyState _state;

        public AltPeekKeyProcessor(AltKeyState state)
        {
            _state = state;
        }

        internal override void UpdateState(KeyEventArgs args)
        {
            _state.Enabled = (args.KeyboardDevice.Modifiers & ModifierKeys.Alt) != 0 &&
                             (args.KeyboardDevice.Modifiers & ModifierKeys.Shift) == 0;
        }


    }

    [Export(typeof(IMouseProcessorProvider))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType("code")]
    [Name("AltPeek")]
    [Order(Before = "GoToDefMouseHandlerProvider")]
    internal sealed class AltPeekMouseHandlerProvider : MouseHandlerProviderBase, IMouseProcessorProvider
    {
        public IMouseProcessor GetAssociatedProcessor(IWpfTextView view)
        {
            var buffer = view.TextBuffer;

            IOleCommandTarget shellCommandDispatcher = GetShellCommandDispatcher(view);

            if (shellCommandDispatcher == null)
                return null;

            return new AltPeekMouseHandler(view,
                                           shellCommandDispatcher,
                                           AggregatorFactory.GetClassifier(buffer),
                                           NavigatorService.GetTextStructureNavigator(buffer),
                                           AltKeyState.GetStateForView(view));
        }
    }

    /// <summary>
    /// Handle ctrl+click on valid elements to send GoToDefinition to the shell.  Also handle mouse moves
    /// (when control is pressed) to highlight references for which GoToDefinition will (likely) be valid.
    /// </summary>
    internal sealed class AltPeekMouseHandler : MouseHandlerBase
    {
        

        public AltPeekMouseHandler(IWpfTextView view, IOleCommandTarget commandTarget, IClassifier aggregator,
                                   ITextStructureNavigator navigator, IKeyState state) : base(view, commandTarget, aggregator, navigator, state)
        {
        }

        internal override bool Dispatch()
        {
            Guid cmdGroup = Microsoft.VisualStudio.VSConstants.CMDSETID.StandardCommandSet12_guid;
            int hr = _commandTarget.Exec(ref cmdGroup,
                                         (uint)VSConstants.VSStd12CmdID.PeekDefinition,
                                         (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT,
                                         System.IntPtr.Zero,
                                         System.IntPtr.Zero);
            return ErrorHandler.Succeeded(hr);
        }

    }
}

