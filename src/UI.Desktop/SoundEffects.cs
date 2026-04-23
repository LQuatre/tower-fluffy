using System;
using System.IO;
using NetCoreAudio;

namespace TowerFluffy.UI.Desktop;

public static class SoundEffects
{
    private static readonly Player _player = new Player();
    private static string? _themePath;

    public static void Initialize()
    {
        var baseDir = AppContext.BaseDirectory;
        _themePath = Path.Combine(baseDir, "Assets", "Sounds", "theme_futur.mp3");
        
        if (!File.Exists(_themePath))
        {
            Console.WriteLine($"[Audio] Erreur : Fichier introuvable {_themePath}");
        }
    }

    public static void PlayTheme()
    {
        if (_themePath != null && File.Exists(_themePath))
        {
            _player.Play(_themePath).ContinueWith(t => {
                if (t.IsCompleted) PlayTheme();
            });
        }
    }

    public static void StopTheme()
    {
        _player.Stop();
    }

    public static void PlayLaser() {}
    public static void PlayFlame() {}
    public static void PlayFlamme() {} // Alias pour compatibilité
    public static void PlayPiou() {}   // Attendu par le ViewModel
}
