using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsNotes.Models;
using WindowsNotes.Services;

namespace WindowsNotes
{
    // Simple shared state so every window can save the same collection
    public static class AppState
    {
        public static System.Collections.Generic.List<Note> AllNotes { get; set; } = new();
    }

    public partial class MainWindow : Window
    {
        private readonly NotesService _service = new();
        private readonly ObservableCollection<Note> _filtered = new();

        public MainWindow()
        {
            InitializeComponent();

            AppState.AllNotes = _service.Load()
                .OrderByDescending(n => n.UpdatedUtc)
                .ToList();

            foreach (var n in AppState.AllNotes) _filtered.Add(n);
            NotesList.ItemsSource = _filtered;

            Closing += OnClosing;
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            _service.Save(AppState.AllNotes);
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            var note = new Note { Title = "New note" };
            AppState.AllNotes.Insert(0, note);
            _filtered.Insert(0, note);
            _service.Save(AppState.AllNotes);

            // Open immediately as a separate window
            OpenNoteWindow(note);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (NotesList.SelectedItem is not Note note) return;
            if (MessageBox.Show("Delete this note?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            AppState.AllNotes.Remove(note);
            _filtered.Remove(note);
            _service.Save(AppState.AllNotes);
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (NotesList.SelectedItem is Note note) OpenNoteWindow(note);
        }

        private void NotesList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (NotesList.SelectedItem is Note note) OpenNoteWindow(note);
        }

        private void OpenNoteWindow(Note note)
        {
            var win = new NoteWindow(note, _service);
            win.Show(); // modeless; multiple can be open
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
            _filtered.Clear();

            var src = AppState.AllNotes.AsEnumerable();
            if (!string.IsNullOrEmpty(q))
            {
                src = src.Where(n =>
                    (n.Title ?? "").ToLowerInvariant().Contains(q) ||
                    (n.Body ?? "").ToLowerInvariant().Contains(q));
            }

            foreach (var n in src) _filtered.Add(n);
        }
    }
}
