using System.Windows.Controls;

namespace EgeTracker.Services
{
    /// <summary>
    /// Управление режимом InkCanvas: маркер / ластик / выкл.
    /// </summary>
    public static class InkToolService
    {
        public static void SetInk(InkCanvas canvas)
        {
            canvas.EditingMode = InkCanvasEditingMode.Ink;
            canvas.IsHitTestVisible = true;
        }

        public static void SetEraser(InkCanvas canvas)
        {
            canvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
            canvas.IsHitTestVisible = true;
        }

        public static void Disable(InkCanvas canvas)
        {
            canvas.EditingMode = InkCanvasEditingMode.None;
            canvas.IsHitTestVisible = false;
        }

        public static void Clear(InkCanvas canvas)
        {
            canvas.Strokes.Clear();
        }
    }
}