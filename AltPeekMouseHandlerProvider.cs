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

    [Export(typeof(IMouseProcessorProvider))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType("code")]
    [Name("AltPeek")]
    [Order(Before = "GoToDefMouseHandlerProvider")]
    internal sealed class AltPeekMouseHandlerProvider : IMouseProcessorProvider
    {
        [Import]
        IClassifierAggregatorService AggregatorFactory = null;

        [Import]
        ITextStructureNavigatorSelectorService NavigatorService = null;

        [Import]
        SVsServiceProvider GlobalServiceProvider = null;

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

        #region Private helpers

        /// <summary>
        /// Get the SUIHostCommandDispatcher from the global service provider.
        /// </summary>
        IOleCommandTarget GetShellCommandDispatcher(ITextView view)
        {
            return GlobalServiceProvider.GetService(typeof(SUIHostCommandDispatcher)) as IOleCommandTarget;
        }

        #endregion
    }
}
