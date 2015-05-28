using System.ComponentModel.Composition;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Shell;

namespace GoToDef
{
    internal class MouseHandlerProviderBase
    {
        [Import]
        protected IClassifierAggregatorService AggregatorFactory = null;

        [Import]
        protected ITextStructureNavigatorSelectorService NavigatorService = null;

        [Import]
        protected SVsServiceProvider GlobalServiceProvider = null;

        #region Private helpers

        /// <summary>
        /// Get the SUIHostCommandDispatcher from the global service provider.
        /// </summary>
        protected IOleCommandTarget GetShellCommandDispatcher(ITextView view)
        {
            return GlobalServiceProvider.GetService(typeof(SUIHostCommandDispatcher)) as IOleCommandTarget;
        }

        #endregion
    }
}
