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
    [Name("GotoDef")]
    [Order(Before = "VisualStudioKeyboardProcessor")]
    internal sealed class GoToDefKeyProcessorProvider : IKeyProcessorProvider
    {
        public KeyProcessor GetAssociatedProcessor(IWpfTextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty(typeof(GoToDefKeyProcessor),
                                                                () => new GoToDefKeyProcessor(CtrlKeyState.GetStateForView(view)));
        }
    }

    /// <summary>
    /// The state of the control key for a given view, which is kept up-to-date by a combination of the
    /// key processor and the mouse process
    /// </summary>
    internal sealed class CtrlKeyState : IKeyState
    {
        internal static CtrlKeyState GetStateForView(ITextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty(typeof(CtrlKeyState), () => new CtrlKeyState());
        }

        bool _enabled = false;

        public bool Enabled
        {
            get
            {
                // Check and see if ctrl is down but we missed it somehow.
                bool ctrlDown = (Keyboard.Modifiers & ModifierKeys.Control) != 0 &&
                                (Keyboard.Modifiers & ModifierKeys.Shift) == 0;
                if (ctrlDown != _enabled)
                    Enabled = ctrlDown;

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
    /// Listen for the control key being pressed or released to update the CtrlKeyStateChanged for a view.
    /// </summary>
    internal sealed class GoToDefKeyProcessor : KeyProcessorBase
    {
        CtrlKeyState _state;

        public GoToDefKeyProcessor(CtrlKeyState state)
        {
            _state = state;
        }

        internal override void UpdateState(KeyEventArgs args)
        {
            _state.Enabled = (args.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0 &&
                             (args.KeyboardDevice.Modifiers & ModifierKeys.Shift) == 0;
        }
    }

    [Export(typeof(IMouseProcessorProvider))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType("code")]
    [Name("GotoDef")]
    [Order(Before = "WordSelection")]
    internal sealed class GoToDefMouseHandlerProvider : MouseHandlerProviderBase, IMouseProcessorProvider
    {
        public IMouseProcessor GetAssociatedProcessor(IWpfTextView view)
        {
            var buffer = view.TextBuffer;

            IOleCommandTarget shellCommandDispatcher = GetShellCommandDispatcher(view);

            if (shellCommandDispatcher == null)
                return null;

            return new GoToDefMouseHandler(view,
                                           shellCommandDispatcher,
                                           AggregatorFactory.GetClassifier(buffer),
                                           NavigatorService.GetTextStructureNavigator(buffer),
                                           CtrlKeyState.GetStateForView(view));
        }
    }

    /// <summary>
    /// Handle ctrl+click on valid elements to send GoToDefinition to the shell.  Also handle mouse moves
    /// (when control is pressed) to highlight references for which GoToDefinition will (likely) be valid.
    /// </summary>
    internal sealed class GoToDefMouseHandler : MouseHandlerBase
    {
        public GoToDefMouseHandler(IWpfTextView view, IOleCommandTarget commandTarget, IClassifier aggregator,
                                   ITextStructureNavigator navigator, IKeyState state) : base(view, commandTarget, aggregator, navigator, state)
        {
        }

        internal override bool Dispatch()
        {
            Guid cmdGroup = VSConstants.GUID_VSStandardCommandSet97;
            int hr = _commandTarget.Exec(ref cmdGroup,
                                         (uint)VSConstants.VSStd97CmdID.GotoDefn,
                                         (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT,
                                         System.IntPtr.Zero,
                                         System.IntPtr.Zero);
            return ErrorHandler.Succeeded(hr);
        }
    }
}
