using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace GoToDef
{
    internal abstract class MouseHandlerBase : MouseProcessorBase
    {
        protected IWpfTextView _view;
        protected IClassifier _aggregator;
        protected ITextStructureNavigator _navigator;
        protected IOleCommandTarget _commandTarget;
        protected IKeyState _state;

        public MouseHandlerBase(IWpfTextView view, IOleCommandTarget commandTarget, IClassifier aggregator,ITextStructureNavigator navigator, IKeyState state)
        {
            _view = view;
            _commandTarget = commandTarget;
            _aggregator = aggregator;
            _navigator = navigator;
            _state = state;

            _view.LostAggregateFocus += (sender, args) => this.SetHighlightSpan(null);
            _view.VisualElement.MouseLeave += (sender, args) => this.SetHighlightSpan(null);

            _state.KeyStateChanged += (sender, args) =>
            {
                if (_state.Enabled)
                    this.TryHighlightItemUnderMouse(RelativeToView(Mouse.PrimaryDevice.GetPosition(_view.VisualElement)));
                else
                    this.SetHighlightSpan(null);
            };
        }

        protected internal bool SetHighlightSpan(SnapshotSpan? span)
        {
            var classifier = UnderlineClassifierProvider.GetClassifierForView(_view);
            if (classifier != null)
            {
                if (span.HasValue)
                    Mouse.OverrideCursor = Cursors.Hand;
                else
                    Mouse.OverrideCursor = null;

                classifier.SetUnderlineSpan(span);
                return true;
            }

            return false;
        }

        #region Mouse processor overrides

        // Remember the location of the mouse on left button down, so we only handle left button up
        // if the mouse has stayed in a single location.
        protected Point? _mouseDownAnchorPoint;

        public override void PostprocessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _mouseDownAnchorPoint = RelativeToView(e.GetPosition(_view.VisualElement));
        }

        protected bool InDragOperation(Point anchorPoint, Point currentPoint)
        {
            // If the mouse up is more than a drag away from the mouse down, this is a drag
            return Math.Abs(anchorPoint.X - currentPoint.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                   Math.Abs(anchorPoint.Y - currentPoint.Y) >= SystemParameters.MinimumVerticalDragDistance;
        }

        public override void PreprocessMouseMove(MouseEventArgs e)
        {
            if (!_mouseDownAnchorPoint.HasValue && _state.Enabled && e.LeftButton == MouseButtonState.Released)
            {
                TryHighlightItemUnderMouse(RelativeToView(e.GetPosition(_view.VisualElement)));
            }
            else if (_mouseDownAnchorPoint.HasValue)
            {
                // Check and see if this is a drag; if so, clear out the highlight.
                var currentMousePosition = RelativeToView(e.GetPosition(_view.VisualElement));
                if (InDragOperation(_mouseDownAnchorPoint.Value, currentMousePosition))
                {
                    _mouseDownAnchorPoint = null;
                    this.SetHighlightSpan(null);
                }
            }
        }

        public override void PreprocessMouseUp(MouseButtonEventArgs e)
        {
            if (_mouseDownAnchorPoint.HasValue && _state.Enabled)
            {
                var currentMousePosition = RelativeToView(e.GetPosition(_view.VisualElement));

                if (!InDragOperation(_mouseDownAnchorPoint.Value, currentMousePosition))
                {
                    _state.Enabled = false;

                    this.SetHighlightSpan(null);
                    _view.Selection.Clear();
                    this.Dispatch();

                    e.Handled = true;
                }
            }

            _mouseDownAnchorPoint = null;
        }

        public override void PreprocessMouseLeave(MouseEventArgs e)
        {
            _mouseDownAnchorPoint = null;
        }

        #endregion

        #region Private helpers

        protected Point RelativeToView(Point position)
        {
            return new Point(position.X + _view.ViewportLeft, position.Y + _view.ViewportTop);
        }

        protected bool TryHighlightItemUnderMouse(Point position)
        {
            bool updated = false;

            try
            {
                var line = _view.TextViewLines.GetTextViewLineContainingYCoordinate(position.Y);
                if (line == null)
                    return false;

                var bufferPosition = line.GetBufferPositionFromXCoordinate(position.X);

                if (!bufferPosition.HasValue)
                    return false;

                // Quick check - if the mouse is still inside the current underline span, we're already set
                var currentSpan = CurrentUnderlineSpan;
                if (currentSpan.HasValue && currentSpan.Value.Contains(bufferPosition.Value))
                {
                    updated = true;
                    return true;
                }

                var extent = _navigator.GetExtentOfWord(bufferPosition.Value);
                if (!extent.IsSignificant)
                    return false;

                // For C#, we ignore namespaces after using statements - GoToDef will fail for those
                if (_view.TextBuffer.ContentType.IsOfType("csharp"))
                {
                    string lineText = bufferPosition.Value.GetContainingLine().GetText().Trim();
                    if (lineText.StartsWith("using", StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                //  Now, check for valid classification type.  C# and C++ (at least) classify the things we are interested
                // in as either "identifier" or "user types" (though "identifier" will yield some false positives).  VB, unfortunately,
                // doesn't classify identifiers.
                foreach (var classification in _aggregator.GetClassificationSpans(extent.Span))
                {
                    var name = classification.ClassificationType.Classification.ToLower();
                    if ((name.Contains("identifier") || name.Contains("user types")) &&
                        SetHighlightSpan(classification.Span))
                    {
                        updated = true;
                        return true;
                    }
                }

                // No update occurred, so return false
                return false;
            }
            finally
            {
                if (!updated)
                    SetHighlightSpan(null);
            }
        }

        private SnapshotSpan? CurrentUnderlineSpan
        {
            get
            {
                var classifier = UnderlineClassifierProvider.GetClassifierForView(_view);
                if (classifier != null && classifier.CurrentUnderlineSpan.HasValue)
                    return classifier.CurrentUnderlineSpan.Value.TranslateTo(_view.TextSnapshot, SpanTrackingMode.EdgeExclusive);
                else
                    return null;
            }
        }

        #endregion

        internal abstract bool Dispatch();
    }
}
