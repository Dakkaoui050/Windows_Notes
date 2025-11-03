using System.Configuration;
using System.Data;
using System.Windows;
using WindowsNotes.Models;
using WindowsNotes.Services;

namespace WindowsNotes;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private NotesService _service;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _service = new NotesService();

        // load notes from disk into shared state
        AppState.AllNotes = _service.Load()
            .OrderByDescending(n => n.UpdatedUtc)
            .ToList();

        // CASE 1: normal launch (user double-clicks app)
        // -> show MainWindow and let user work
        // CASE 2: silent autostart (Windows startup)
        //
        // Easiest way: we'll use a command-line flag.
        // If Windows starts us with "--startup", we open all in mini.
        // If user opens us normally, we show MainWindow.

        bool startupMode = e.Args.Any(arg =>
            string.Equals(arg, "--startup", StringComparison.OrdinalIgnoreCase));

        if (startupMode)
        {
            // No MainWindow. Just spawn every note as mini sticky.
            foreach (var note in AppState.AllNotes)
            {
                var win = new NoteWindow(note, _service);

                // position restore (already handled in NoteWindow ctor
                // because you load note.WindowLeft/Top/Width/Height)

                win.Show();

                // force mini mode:
                // we call a helper that mimics clicking the "_" button.
                ForceMini(win);
            }
        }
        else
        {
            // Normal manual open -> show the manager window
            var main = new MainWindow();
            main.Show();
        }
    }

    private void ForceMini(NoteWindow w)
    {
        // we want same behavior as EnterMiniMode(),
        // but EnterMiniMode() is private.
        // EITHER: make EnterMiniMode() public in NoteWindow
        // OR: call the minimize button click handler.

        // Easiest: make EnterMiniMode() public in NoteWindow
        w.ForceMiniModeFromApp();
    }
}

