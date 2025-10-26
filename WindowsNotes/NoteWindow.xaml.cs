using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WindowsNotes.Models;
using WindowsNotes.Services;
using static WindowsNotes.MainWindow;

namespace WindowsNotes
{
    public partial class NoteWindow : Window
    {
        private readonly Note _note;
        private readonly NotesService _service;

        // debounce for saving
        private readonly System.Timers.Timer _saveTimer = new(700) { AutoReset = false };

        // state flags
        private bool _loading;
        private bool _isMinimizedView;
        private double _lastHeight;
        private double _lastWidth;

        public NoteWindow(Note note, NotesService service)
        {
            InitializeComponent();
            // --- Make the title draggable ---
            // --- Make the title draggable ---
            // drag the window by dragging the title box
          

            // Also allow dragging by clicking background (outside text areas)
            MouseLeftButtonDown += (_, args) =>
            {
                // ignore if click is on a TextBox caret area (typing intent)
                if (args.Source is TextBox) return;

                if (args.ButtonState == MouseButtonState.Pressed)
                    DragMove();
            };



            _note = note;
            _service = service;

            // fill UI
            _loading = true;
            TitleBox.Text = _note.Title;
            BodyBox.Text = _note.Body;

            if (_note.WindowWidth is double w) Width = w;
            if (_note.WindowHeight is double h) Height = h;
            if (_note.WindowLeft is double l) Left = l;
            if (_note.WindowTop is double t) Top = t;

            // you had TopmostBox before, but we removed it from XAML for now.
           

            Title = string.IsNullOrWhiteSpace(_note.Title) ? "Note" : _note.Title;
            _loading = false;

            // debounced save
            _saveTimer.Elapsed += (s, e) =>
            {
                Dispatcher.Invoke(() => _service.Save(AppState.AllNotes));
            };

            // text changed handlers
            TitleBox.TextChanged += OnTitleChanged;
            BodyBox.TextChanged += OnBodyChanged;

            // drag the window by dragging the title bar / header
            TitleBox.PreviewMouseLeftButtonDown += (sender, args) =>
            {
                // In MINI MODE:
                // - Title is read-only
                // - Dragging the title should move the sticky
                if (_isMinimizedView)
                {
                    if (args.ButtonState == MouseButtonState.Pressed)
                    {
                        DragMove();
                        args.Handled = true; // don't try to focus textbox
                    }
                    return;
                }

                // NORMAL MODE:
                // We want to be able to type in the title.
                // So: do NOT drag here. Let the TextBox behave normally.
                // (No code needed here.)
            };


            // When the whole sticky is dragged from the blank background (not just title),
            // allow moving too:
            MouseLeftButtonDown += (_, args) =>
            {
                // ignore if click is on a TextBox caret area (user wants to type)
                if (args.Source is TextBox) return;

                if (_isMinimizedView)
                {
                    // in mini mode: clicking background should also allow drag
                    if (args.ButtonState == MouseButtonState.Pressed)
                        DragMove();
                    return;
                }

                if (args.ButtonState == MouseButtonState.Pressed)
                    DragMove();
            };

            // save geometry when moved / resized / closed
            LocationChanged += (_, __) =>
            {
                ClampToScreen();
                SaveWindowGeometryDelayed();
            };

            SizeChanged += (_, __) => SaveWindowGeometryDelayed();
            Closed += (_, __) => SaveWindowGeometryImmediate();
        }

        // Instead of subscribing to StateChanged in constructor,
        // we override OnStateChanged so we can intercept Minimized
        protected override void OnStateChanged(EventArgs e)
        {
            // User hits minimize button
            if (WindowState == WindowState.Minimized)
            {
                // cancel the real minimize
                WindowState = WindowState.Normal;
                EnterMiniMode();
                return;
            }

            base.OnStateChanged(e);
        }
        private void MiniButton_Click(object sender, RoutedEventArgs e)
        {
            EnterMiniMode();
        }
        // --- typing in title updates model + window title ---
        private void OnTitleChanged(object sender, TextChangedEventArgs e)
        {
            if (_loading) return;

            _note.Title = TitleBox.Text;
            _note.UpdatedUtc = DateTime.UtcNow;

            Title = string.IsNullOrWhiteSpace(_note.Title) ? "Note" : _note.Title;

            KickSaveDebounce();
        }

        // --- typing in body updates model ---
        private void OnBodyChanged(object sender, TextChangedEventArgs e)
        {
            if (_loading) return;

            _note.Body = BodyBox.Text;
            _note.UpdatedUtc = DateTime.UtcNow;

            KickSaveDebounce();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // --- always on top checkbox ---
        private void SetTopmost(bool value)
        {
            Topmost = value;
            _note.AlwaysOnTop = value;
            KickSaveDebounce();
        }

        // --- window geometry persistence ---
        private void SaveWindowGeometry()
        {
            _note.WindowLeft = Left;
            _note.WindowTop = Top;
            _note.WindowWidth = Width;
            _note.WindowHeight = Height;
        }

        private void SaveWindowGeometryImmediate()
        {
            SaveWindowGeometry();
            _service.Save(AppState.AllNotes);
        }

        private void SaveWindowGeometryDelayed()
        {
            SaveWindowGeometry();
            KickSaveDebounce();
        }

        // --- save debounce timer ---
        private void KickSaveDebounce()
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        // -------------------------------------------------
        // MINI MODE (sticky bar mode)
        // -------------------------------------------------
        private void EnterMiniMode()
        {
            if (_isMinimizedView) return;
            _isMinimizedView = true;

            // save previous size
            _lastHeight = Height;
            _lastWidth = Width;

            // hide the body so only header stays visible
            BodyBox.Visibility = Visibility.Collapsed;

            // shrink window
            Height = 50;
            Width = 220;

            // lock resize in mini mode
            ResizeMode = ResizeMode.NoResize;

            // always on top (so you don't "lose" the mini sticky)
            Topmost = true;

            // UI state for buttons:
            // when minimized: hide "_" (already mini), show restore "▢"
            MiniButton.Visibility = Visibility.Collapsed;
            RestoreButton.Visibility = Visibility.Visible;

            // make the title non-editable and look like a label
            TitleBox.IsReadOnly = true;
            TitleBox.BorderThickness = new Thickness(0);
            TitleBox.Background = Brushes.Yellow;
        }

        private void ExitMiniMode()
        {
            if (!_isMinimizedView) return;
            _isMinimizedView = false;

            // show body again
            BodyBox.Visibility = Visibility.Visible;

            // restore size
            Height = _lastHeight;
            Width = _lastWidth;

            // unlock resize
            ResizeMode = ResizeMode.CanResizeWithGrip;

            // keep topmost for now
            Topmost = true;

            // UI state for buttons:
            // when normal: show "_" (can minimize again), hide "▢"
            MiniButton.Visibility = Visibility.Visible;
            RestoreButton.Visibility = Visibility.Collapsed;
           
            TitleBox.IsReadOnly = false;
            TitleBox.Background = Brushes.Yellow; // still yellow
            ClampToScreen(); // keep it visible after restore
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            ExitMiniMode();
        }
        private void ClampToScreen()
        {
            // Work area = full screen minus taskbar etc.
            Rect screen = SystemParameters.WorkArea;

            // Clamp horizontal position
            if (Left < screen.Left)
                Left = screen.Left;
            if (Left + Width > screen.Right)
                Left = screen.Right - Width;

            // Clamp vertical position
            if (Top < screen.Top)
                Top = screen.Top;
            if (Top + Height > screen.Bottom)
                Top = screen.Bottom - Height;
        }


    }
}

