using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;

namespace GoToDef
{
    internal abstract class KeyProcessorBase : KeyProcessor
    {
        internal abstract void UpdateState(KeyEventArgs args);

        public override void PreviewKeyDown(KeyEventArgs args)
        {
            UpdateState(args);
        }

        public override void PreviewKeyUp(KeyEventArgs args)
        {
            UpdateState(args);
        }
    }
}
