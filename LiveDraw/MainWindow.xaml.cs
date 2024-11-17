using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

namespace AntFu7.LiveDraw
{
    public partial class MainWindow : Window
    {
        private int _eraseByPointFlag = 0;

        private static readonly Mutex Mutex = new Mutex(true, "LiveDraw");
        private static readonly Duration Duration3 = (Duration)Application.Current.Resources["Duration3"];
        private static readonly Duration Duration4 = (Duration)Application.Current.Resources["Duration4"];
        private static readonly Duration Duration5 = (Duration)Application.Current.Resources["Duration5"];
        private static readonly Duration Duration7 = (Duration)Application.Current.Resources["Duration7"];

        private const string SaveDirectoryName = "Save";

        public MainWindow()
        {
            if (Mutex.WaitOne(TimeSpan.Zero, true))
            {
                _history = new Stack<StrokesHistoryNode>();
                _redoHistory = new Stack<StrokesHistoryNode>();
                if (!Directory.Exists(SaveDirectoryName))
                {
                    Directory.CreateDirectory(SaveDirectoryName);
                }

                InitializeComponent();
                SetColor(DefaultColorPicker);
                SetEnabled(false);
                SetTopMost(true);
                SetDetailPanel(true);
                SetBrushSize(_brushSizes[_brushIndex]);
                DetailPanel.Opacity = 0;

                MainInkCanvas.Strokes.StrokesChanged += StrokesChanged;
                MainInkCanvas.MouseLeftButtonDown += StartLine;
                MainInkCanvas.MouseLeftButtonUp += EndLine;
                MainInkCanvas.MouseMove += MakeLine;
                MainInkCanvas.MouseWheel += BrushSize;
            }
            else
            {
                Application.Current.Shutdown(0);
            }
        }

        private void Exit(object? sender, EventArgs e)
        {
            if (IsUnsaved())
            {
                QuickSave("ExitingAutoSave_");
            }

            Application.Current.Shutdown(0);
        }

        private bool _saved;

        private bool IsUnsaved()
        {
            return MainInkCanvas.Strokes.Count != 0 && !_saved;
        }

        private bool PromptToSave()
        {
            if (!IsUnsaved())
            {
                return true;
            }

            var r = MessageBox.Show("You have unsaved work, do you want to save it now?", "Unsaved data", MessageBoxButton.YesNoCancel);
            if (r is not (MessageBoxResult.Yes or MessageBoxResult.OK))
            {
                return r is MessageBoxResult.No or MessageBoxResult.None;
            }

            QuickSave();
            return true;
        }

        private ColorPickerButton? _selectedColor;
        private bool _inkVisibility = true;
        private bool _displayDetailPanel;
        private bool _isInEraserMode;
        private bool _enabled;
        private readonly int[] _brushSizes = [3, 5, 8, 13, 20];
        private int _brushIndex = 1;
        private bool _displayOrientation;

        private void SetDetailPanel(bool v)
        {
            if (v)
            {
                DetailTogglerRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(180, Duration5));
                DetailPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, Duration4));
            }
            else
            {
                DetailTogglerRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, Duration5));
                DetailPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, Duration4));
            }
            _displayDetailPanel = v;
        }

        private void SetInkVisibility(bool v)
        {
            MainInkCanvas.BeginAnimation(OpacityProperty, v ? new DoubleAnimation(0, 1, Duration3) : new DoubleAnimation(1, 0, Duration3));
            HideButton.IsActived = !v;
            SetEnabled(v);
            _inkVisibility = v;
        }

        private void SetEnabled(bool isEnabled)
        {
            EnableButton.IsActived = !isEnabled;
            Background = Application.Current.Resources[isEnabled ? "FakeTransparent" : "TrueTransparent"] as Brush;
            _enabled = isEnabled;
            MainInkCanvas.UseCustomCursor = false;

            if (_enabled)
            {
                LineButton.IsActived = false;
                EraserButton.IsActived = false;
                SetStaticInfo("LiveDraw");
                MainInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
            else
            {
                SetStaticInfo("Locked");
                MainInkCanvas.EditingMode = InkCanvasEditingMode.None; //No inking possible
            }
        }

        private void SetColor(ColorPickerButton bColorPicker)
        {
            if (ReferenceEquals(_selectedColor, bColorPicker))
            {
                return;
            }

            if (bColorPicker.Background is not SolidColorBrush solidColorBrush)
            {
                return;
            }

            var ani = new ColorAnimation(solidColorBrush.Color, Duration3);

            MainInkCanvas.DefaultDrawingAttributes.Color = solidColorBrush.Color;
            BrushPreview.Background.BeginAnimation(SolidColorBrush.ColorProperty, ani);
            bColorPicker.IsActived = true;
            if (_selectedColor != null)
            {
                _selectedColor.IsActived = false;
            }

            _selectedColor = bColorPicker;
        }

        private void SetBrushSize(double size)
        {
            if (MainInkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
            {
                MainInkCanvas.EditingMode = InkCanvasEditingMode.GestureOnly;
                MainInkCanvas.EraserShape = new EllipseStylusShape(size, size);
                MainInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            }
            else
            {
                MainInkCanvas.DefaultDrawingAttributes.Height = size;
                MainInkCanvas.DefaultDrawingAttributes.Width = size;
                BrushPreview?.BeginAnimation(HeightProperty, new DoubleAnimation(size, Duration4));
                BrushPreview?.BeginAnimation(WidthProperty, new DoubleAnimation(size, Duration4));
            }
        }

        private void SetEraserMode(bool v)
        {
            EraserButton.IsActived = v;
            _isInEraserMode = v;
            MainInkCanvas.UseCustomCursor = false;

            if (_isInEraserMode)
            {
                MainInkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                SetStaticInfo("Eraser Mode");
            }
            else
            {
                SetEnabled(_enabled);
            }
        }

        private void SetOrientation(bool v)
        {
            PaletteRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(v ? -90 : 0, Duration4));
            Palette.BeginAnimation(MinWidthProperty, new DoubleAnimation(v ? 90 : 0, Duration7));
            _displayOrientation = v;
        }

        private void SetTopMost(bool isTopMost)
        {
            PinButton.IsActived = isTopMost;
            Topmost = isTopMost;
        }

        private StrokeCollection? _preLoadStrokes;

        private void QuickSave(string filename = "QuickSave_")
        {
            Save(new FileStream(Path.Combine(SaveDirectoryName, filename + GenerateFileName()), FileMode.OpenOrCreate));
        }

        private async void Save(Stream fs)
        {
            try
            {
                if (fs == Stream.Null)
                {
                    return;
                }

                using (fs)
                {
                    MainInkCanvas.Strokes.Save(fs);
                    _saved = true;
                    await Display("Ink saved");
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                await Display("Fail to save");
            }
        }

        private async Task<StrokeCollection> Load(Stream fs)
        {
            try
            {
                return new StrokeCollection(fs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                await Display("Fail to load");
            }

            return [];
        }

        private void AnimatedReload(StrokeCollection sc)
        {
            _preLoadStrokes = sc;
            var ani = new DoubleAnimation(0, Duration3);
            ani.Completed += LoadAniCompleted;
            MainInkCanvas.BeginAnimation(OpacityProperty, ani);
        }

        private async void LoadAniCompleted(object? sender, EventArgs e)
        {
            if (_preLoadStrokes == null)
            {
                return;
            }

            MainInkCanvas.Strokes = _preLoadStrokes;
            await Display("Ink loaded");
            _saved = true;
            ClearHistory();
            MainInkCanvas.BeginAnimation(OpacityProperty, new DoubleAnimation(1, Duration3));
        }

        private static string GenerateFileName(string fileExt = ".fdw")
        {
            return DateTime.Now.ToString("yyyyMMdd-HHmmss") + fileExt;
        }

        private string _staticInfo = "";
        private bool _displayingInfo;

        private async Task Display(string info)
        {
            InfoBox.Text = info;
            _displayingInfo = true;
            await InfoDisplayTimeUp(new Progress<string>(box => InfoBox.Text = box));
        }

        private Task InfoDisplayTimeUp(IProgress<string> box)
        {
            return Task.Run(async () =>
            {
                await Task.Delay(2000);
                box.Report(_staticInfo);
                _displayingInfo = false;
            });
        }

        private void SetStaticInfo(string info)
        {
            _staticInfo = info;
            if (!_displayingInfo)
            {
                InfoBox.Text = _staticInfo;
            }
        }

        private static Stream SaveDialog(string initFileName, string fileExt = ".fdw", string filter = "Free Draw Save (*.fdw)|*fdw")
        {
            var dialog = new Microsoft.Win32.SaveFileDialog()
            {
                DefaultExt = fileExt,
                Filter = filter,
                FileName = initFileName,
                InitialDirectory = Directory.GetCurrentDirectory() + SaveDirectoryName
            };
            return dialog.ShowDialog() == true ? dialog.OpenFile() : Stream.Null;
        }

        private static Stream OpenDialog(string fileExt = ".fdw", string filter = "Free Draw Save (*.fdw)|*fdw")
        {
            var dialog = new Microsoft.Win32.OpenFileDialog()
            {
                DefaultExt = fileExt,
                Filter = filter,
            };
            return dialog.ShowDialog() == true ? dialog.OpenFile() : Stream.Null;
        }

        private void EraserFunction()
        {
            LineMode(false);
            switch (_eraseByPointFlag)
            {
                case (int)EraseMode.None:
                    SetEraserMode(!_isInEraserMode);
                    EraserButton.ToolTip = "Toggle eraser (by point) mode (D)";
                    _eraseByPointFlag = (int)EraseMode.Eraser;
                    break;

                case (int)EraseMode.Eraser:
                    {
                        EraserButton.IsActived = true;
                        SetStaticInfo("Eraser Mode (Point)");
                        EraserButton.ToolTip = "Toggle eraser - OFF";
                        var s = MainInkCanvas.EraserShape.Height;
                        MainInkCanvas.EraserShape = new EllipseStylusShape(s, s);
                        MainInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                        _eraseByPointFlag = (int)EraseMode.EraserByPoint;
                        break;
                    }
                case (int)EraseMode.EraserByPoint:
                    SetEraserMode(!_isInEraserMode);
                    EraserButton.ToolTip = "Toggle eraser mode (E)";
                    _eraseByPointFlag = (int)EraseMode.None;
                    break;
            }
        }

        private readonly Stack<StrokesHistoryNode> _history;
        private readonly Stack<StrokesHistoryNode> _redoHistory;
        private bool _ignoreStrokesChange;

        private void Undo()
        {
            if (!CanUndo())
            {
                return;
            }

            var last = Pop(_history);
            if (last is null)
            {
                return;
            }

            _ignoreStrokesChange = true;
            if (last.Type == StrokesHistoryNodeType.Added)
            {
                MainInkCanvas.Strokes.Remove(last.Strokes);
            }
            else
            {
                MainInkCanvas.Strokes.Add(last.Strokes);
            }

            _ignoreStrokesChange = false;
            Push(_redoHistory, last);
        }

        private void Redo()
        {
            if (!CanRedo())
            {
                return;
            }

            var last = Pop(_redoHistory);
            if (last is null)
            {
                return;
            }

            _ignoreStrokesChange = true;
            if (last.Type == StrokesHistoryNodeType.Removed)
            {
                MainInkCanvas.Strokes.Remove(last.Strokes);
            }
            else
            {
                MainInkCanvas.Strokes.Add(last.Strokes);
            }

            _ignoreStrokesChange = false;
            Push(_history, last);
        }

        private static void Push(Stack<StrokesHistoryNode> collection, StrokesHistoryNode node)
        {
            collection.Push(node);
        }

        private static StrokesHistoryNode? Pop(Stack<StrokesHistoryNode> collection)
        {
            return collection.Count == 0 ? null : collection.Pop();
        }

        private bool CanUndo()
        {
            return _history.Count != 0;
        }

        private bool CanRedo()
        {
            return _redoHistory.Count != 0;
        }

        private void StrokesChanged(object? sender, StrokeCollectionChangedEventArgs e)
        {
            if (_ignoreStrokesChange)
            {
                return;
            }

            _saved = false;
            if (e.Added.Count != 0)
            {
                Push(_history, new StrokesHistoryNode(e.Added, StrokesHistoryNodeType.Added));
            }

            if (e.Removed.Count != 0)
            {
                Push(_history, new StrokesHistoryNode(e.Removed, StrokesHistoryNodeType.Removed));
            }

            ClearHistory(_redoHistory);
        }

        private void ClearHistory()
        {
            ClearHistory(_history);
            ClearHistory(_redoHistory);
        }

        private static void ClearHistory(Stack<StrokesHistoryNode> collection)
        {
            collection?.Clear();
        }

        private void Clear()
        {
            MainInkCanvas.Strokes.Clear();
            ClearHistory();
        }

        private void AnimatedClear()
        {
            var ani = new DoubleAnimation(0, Duration3);
            ani.Completed += ClearAniComplete;
            MainInkCanvas.BeginAnimation(OpacityProperty, ani);
        }

        private async void ClearAniComplete(object? sender, EventArgs e)
        {
            Clear();
            await Display("Cleared");
            MainInkCanvas.BeginAnimation(OpacityProperty, new DoubleAnimation(1, Duration3));
        }

        private void DetailToggler_Click(object? sender, RoutedEventArgs e)
        {
            SetDetailPanel(!_displayDetailPanel);
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Topmost = false;
            var anim = new DoubleAnimation(0, Duration3);
            anim.Completed += Exit;
            BeginAnimation(OpacityProperty, anim);
        }

        private void ColorPickers_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not ColorPickerButton border)
            {
                return;
            }

            SetColor(border);

            if (_eraseByPointFlag == (int)EraseMode.None)
            {
                return;
            }

            SetEraserMode(false);
            _eraseByPointFlag = (int)EraseMode.None;
            EraserButton.ToolTip = "Toggle eraser mode (E)";
        }

        private void BrushSize(object? sender, MouseWheelEventArgs e)
        {
            var delta = e.Delta;
            if (delta < 0)
            {
                _brushIndex--;
            }
            else
            {
                _brushIndex++;
            }

            if (_brushIndex > _brushSizes.Length - 1)
            {
                _brushIndex = 0;
            }
            else if (_brushIndex < 0)
            {
                _brushIndex = _brushSizes.Length - 1;
            }

            SetBrushSize(_brushSizes[_brushIndex]);
        }

        private void BrushSwitchButton_Click(object? sender, RoutedEventArgs e)
        {
            _brushIndex++;
            if (_brushIndex > _brushSizes.Length - 1)
            {
                _brushIndex = 0;
            }

            SetBrushSize(_brushSizes[_brushIndex]);
        }

        private void LineButton_Click(object? sender, RoutedEventArgs e)
        {
            LineMode(!_lineMode);
        }

        private void UndoButton_Click(object? sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void RedoButton_Click(object? sender, RoutedEventArgs e)
        {
            Redo();
        }

        private void EraserButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_enabled)
            {
                EraserFunction();
            }
        }

        private void ClearButton_Click(object? sender, RoutedEventArgs e)
        {
            AnimatedClear(); //Warning! to missclick erasermode (confirmation click?)
        }

        private void PinButton_Click(object? sender, RoutedEventArgs e)
        {
            SetTopMost(!Topmost);
        }

        private async void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                await Display("Nothing to save");
                return;
            }

            QuickSave();
        }

        private async void SaveButton_RightClick(object? sender, MouseButtonEventArgs e)
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                await Display("Nothing to save");
                return;
            }

            Save(SaveDialog(GenerateFileName()));
        }

        private async void LoadButton_Click(object? sender, RoutedEventArgs e)
        {
            if (!PromptToSave())
            {
                return;
            }

            var stream = OpenDialog();
            if (stream == Stream.Null)
            {
                return;
            }

            AnimatedReload(await Load(stream));
        }

        private async void ExportButton_Click(object? sender, RoutedEventArgs e)
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                await Display("Nothing to save");
                return;
            }
            try
            {
                await using var s = SaveDialog("ImageExport_" + GenerateFileName(".png"), ".png", "Portable Network Graphics (*png)|*png");
                if (s == Stream.Null)
                {
                    return;
                }

                var rtb = new RenderTargetBitmap((int)MainInkCanvas.ActualWidth, (int)MainInkCanvas.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
                rtb.Render(MainInkCanvas);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                encoder.Save(s);
                s.Close();
                await Display("Image Exported");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                await Display("Export failed");
            }
        }

        private delegate void NoArgDelegate();

        private async void ExportButton_RightClick(object? sender, MouseButtonEventArgs e)
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                await Display("Nothing to save");
                return;
            }
            try
            {
                await using var stream = SaveDialog("ImageExportWithBackground_" + GenerateFileName(".png"), ".png", "Portable Network Graphics (*png)|*png");
                if (stream == Stream.Null)
                {
                    return;
                }

                Palette.Opacity = 0;
                Palette.Dispatcher.Invoke(DispatcherPriority.Render, (NoArgDelegate)delegate { });
                await Task.Delay(100);
                var fromHwnd = Graphics.FromHwnd(IntPtr.Zero);
                var w = (int)(SystemParameters.PrimaryScreenWidth * fromHwnd.DpiX / 96.0);
                var h = (int)(SystemParameters.PrimaryScreenHeight * fromHwnd.DpiY / 96.0);
                var image = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics.FromImage(image).CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(w, h), CopyPixelOperation.SourceCopy);
                image.Save(stream, ImageFormat.Png);
                Palette.Opacity = 1;
                stream.Close();
                await Display("Image Exported");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                await Display("Export failed");
            }
        }

        private void MinimizeButton_Click(object? sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void HideButton_Click(object? sender, RoutedEventArgs e)
        {
            SetInkVisibility(!_inkVisibility);
        }

        private void EnableButton_Click(object? sender, RoutedEventArgs e)
        {
            SetEnabled(!_enabled);
            if (!_isInEraserMode)
            {
                return;
            }

            SetEraserMode(!_isInEraserMode);
            EraserButton.ToolTip = "Toggle eraser mode (E)";
            _eraseByPointFlag = (int)EraseMode.None;
        }

        private void OrientationButton_Click(object? sender, RoutedEventArgs e)
        {
            SetOrientation(!_displayOrientation);
        }

        private Point _lastMousePosition;
        private bool _isDragging;
        private bool _tempEnable;

        private void StartDrag()
        {
            _lastMousePosition = Mouse.GetPosition(this);
            _isDragging = true;
            Palette.Background = new SolidColorBrush(Colors.Transparent);
            _tempEnable = _enabled;
            SetEnabled(true);
        }

        private void EndDrag()
        {
            if (_isDragging)
            {
                SetEnabled(_tempEnable);
            }

            _isDragging = false;
            Palette.Background = null;
        }

        private void PaletteGrip_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            StartDrag();
        }

        private void Palette_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isDragging)
            {
                return;
            }

            var currentMousePosition = Mouse.GetPosition(this);
            var offset = currentMousePosition - _lastMousePosition;

            Canvas.SetTop(Palette, Canvas.GetTop(Palette) + offset.Y);
            Canvas.SetLeft(Palette, Canvas.GetLeft(Palette) + offset.X);

            _lastMousePosition = currentMousePosition;
        }

        private void Palette_MouseUp(object? sender, MouseButtonEventArgs e)
        {
            EndDrag();
        }

        private void Palette_MouseLeave(object? sender, MouseEventArgs e)
        {
            EndDrag();
        }

        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.R)
            {
                SetEnabled(!_enabled);
            }

            if (!_enabled)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Z:
                    Undo();
                    break;

                case Key.Y:
                    Redo();
                    break;

                case Key.E:
                    EraserFunction();
                    break;

                case Key.B:
                    if (_isInEraserMode)
                    {
                        SetEraserMode(false);
                    }

                    SetEnabled(true);
                    break;

                case Key.L:
                    if (_isInEraserMode)
                    {
                        SetEraserMode(false);
                    }

                    LineMode(true);
                    break;

                case Key.Add:
                    _brushIndex++;
                    if (_brushIndex > _brushSizes.Length - 1)
                    {
                        _brushIndex = 0;
                    }

                    SetBrushSize(_brushSizes[_brushIndex]);
                    break;

                case Key.Subtract:
                    _brushIndex--;
                    if (_brushIndex < 0)
                    {
                        _brushIndex = _brushSizes.Length - 1;
                    }

                    SetBrushSize(_brushSizes[_brushIndex]);
                    break;
            }
        }

        private bool _isMoving;
        private bool _lineMode;
        private Point _startPoint;
        private Stroke? _lastStroke;

        private void LineMode(bool l)
        {
            if (!_enabled)
            {
                return;
            }

            _lineMode = l;
            if (_lineMode)
            {
                _eraseByPointFlag = (int)EraseMode.EraserByPoint;
                EraserFunction();
                SetEraserMode(false);
                EraserButton.IsActived = false;
                LineButton.IsActived = l;
                SetStaticInfo("LineMode");
                MainInkCanvas.EditingMode = InkCanvasEditingMode.None;
                MainInkCanvas.UseCustomCursor = true;
            }
            else
            {
                SetEnabled(true);
            }
        }

        private void StartLine(object? sender, MouseButtonEventArgs e)
        {
            _isMoving = true;
            _startPoint = e.GetPosition(MainInkCanvas);
            _lastStroke = null;
            _ignoreStrokesChange = true;
        }

        private void EndLine(object? sender, MouseButtonEventArgs e)
        {
            if (_isMoving)
            {
                if (_lastStroke != null)
                {
                    Push(_history, new StrokesHistoryNode([_lastStroke], StrokesHistoryNodeType.Added));
                }
            }
            _isMoving = false;
            _ignoreStrokesChange = false;
        }

        private void MakeLine(object? sender, MouseEventArgs e)
        {
            if (_isMoving == false)
            {
                return;
            }

            var newLine = MainInkCanvas.DefaultDrawingAttributes.Clone();
            newLine.StylusTip = StylusTip.Ellipse;
            newLine.IgnorePressure = true;

            var endPoint = e.GetPosition(MainInkCanvas);

            var pList = new List<Point>
            {
                new Point(_startPoint.X, _startPoint.Y),
                new Point(endPoint.X, endPoint.Y),
            };

            var point = new StylusPointCollection(pList);
            var stroke = new Stroke(point) { DrawingAttributes = newLine, };

            if (_lastStroke != null)
            {
                MainInkCanvas.Strokes.Remove(_lastStroke);
            }

            MainInkCanvas.Strokes.Add(stroke);

            _lastStroke = stroke;
        }
    }
}