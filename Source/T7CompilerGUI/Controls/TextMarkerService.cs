using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace T7CompilerGUI.Controls
{
    /// <summary>
    /// Service for marking text ranges in AvalonEdit (for conditional compilation highlighting)
    /// </summary>
    public class TextMarkerService : DocumentColorizingTransformer, IBackgroundRenderer
    {
        private readonly List<TextMarker> markers = new List<TextMarker>();
        private readonly object lockObject = new object();

        public void Add(TextMarker marker)
        {
            if (marker != null)
            {
                lock (lockObject)
                {
                    markers.Add(marker);
                }
                Redraw(marker);
            }
        }

        public void Remove(TextMarker marker)
        {
            if (marker != null)
            {
                lock (lockObject)
                {
                    markers.Remove(marker);
                }
                Redraw(marker);
            }
        }

        public void RemoveAll(Predicate<TextMarker> predicate)
        {
            List<TextMarker> markersToRemove;
            lock (lockObject)
            {
                markersToRemove = markers.Where(m => predicate(m)).ToList();
                foreach (var marker in markersToRemove)
                {
                    markers.Remove(marker);
                }
            }
            foreach (var marker in markersToRemove)
            {
                Redraw(marker);
            }
        }

        public IEnumerable<TextMarker> GetMarkersAtOffset(int offset)
        {
            lock (lockObject)
            {
                return markers.Where(m => m.StartOffset <= offset && m.EndOffset > offset).ToList();
            }
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (line == null || CurrentContext == null || CurrentContext.Document == null)
                return;

            int lineStart = line.Offset;
            int lineEnd = lineStart + line.Length;

            List<TextMarker> lineMarkers;
            lock (lockObject)
            {
                lineMarkers = markers.Where(m => m.StartOffset < lineEnd && m.EndOffset > lineStart).ToList();
            }

            foreach (var marker in lineMarkers)
            {
                int start = Math.Max(marker.StartOffset, lineStart);
                int end = Math.Min(marker.EndOffset, lineEnd);
                
                if (end > start)
                {
                    if (marker.BackgroundColor != null)
                    {
                        ChangeLinePart(
                            start,
                            end,
                            element => element.BackgroundBrush = new System.Windows.Media.SolidColorBrush(marker.BackgroundColor.Value)
                        );
                    }
                    if (marker.ForegroundColor != null)
                    {
                        ChangeLinePart(
                            start,
                            end,
                            element => element.TextRunProperties.SetForegroundBrush(new System.Windows.Media.SolidColorBrush(marker.ForegroundColor.Value))
                        );
                    }
                }
            }
        }

        public KnownLayer Layer => KnownLayer.Selection;

        public void Draw(TextView textView, System.Windows.Media.DrawingContext drawingContext)
        {
            // Background rendering is handled by ColorizeLine
        }

        private void Redraw(ISegment segment)
        {
            if (CurrentContext != null && CurrentContext.TextView != null)
            {
                CurrentContext.TextView.Redraw(segment);
            }
        }
    }

    public class TextMarker : ISegment
    {
        public TextMarker(int startOffset, int length)
        {
            StartOffset = startOffset;
            Length = length;
        }

        public int StartOffset { get; set; }
        public int Length { get; set; }
        public int EndOffset => StartOffset + Length;
        
        // ISegment interface requires Offset property
        public int Offset
        {
            get => StartOffset;
            set => StartOffset = value;
        }

        public System.Windows.Media.Color? BackgroundColor { get; set; }
        public System.Windows.Media.Color? ForegroundColor { get; set; }
    }
}

