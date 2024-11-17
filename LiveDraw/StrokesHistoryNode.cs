using System.Windows.Ink;

namespace AntFu7.LiveDraw
{
    internal sealed record StrokesHistoryNode(StrokeCollection Strokes, StrokesHistoryNodeType Type);
}