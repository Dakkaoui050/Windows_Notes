using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WindowsNotes.Models;

namespace WindowsNotes.Services
{
    public class NotesService
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _json = new() { WriteIndented = true };

        public NotesService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "WindowsNotes");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "notes.json");
        }

        public List<Note> Load()
        {
            if (!File.Exists(_filePath)) return new List<Note>();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<Note>>(json) ?? new List<Note>();
        }

        public void Save(IEnumerable<Note> notes)
        {
            var json = JsonSerializer.Serialize(notes, _json);
            File.WriteAllText(_filePath, json);
        }
    }
}

