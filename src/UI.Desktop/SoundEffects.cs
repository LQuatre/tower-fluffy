using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using NetCoreAudio;

namespace TowerFluffy.UI.Desktop;

public static class SoundEffects
{
    private static DateTime _lastLaser = DateTime.MinValue;
    private static DateTime _lastFlame = DateTime.MinValue;
    private static readonly object _lock = new();
    
    private static readonly Player _themePlayer = new();
    private static readonly Player _laserPlayer = new();
    private static readonly Player _flamePlayer = new();

    private static string? _laserPath;
    private static string? _flamePath;

    public static void Initialize()
    {
        try
        {
            Console.WriteLine("[Audio] Initialisation du système sonore multiplateforme...");
            
            string? soundsDir = GetSoundsDirectory();
            if (soundsDir == null)
            {
                Console.WriteLine("[Audio] ERREUR : Impossible de localiser le dossier Assets/Sounds.");
                return;
            }

            // 1. Thème
            string themePath = Path.Combine(soundsDir, "theme_futur.mp3");
            if (File.Exists(themePath))
            {
                _themePlayer.Play(themePath);
                // Note: NetCoreAudio ne gère pas nativement le volume ou la boucle facilement sans event
                // Pour simplifier ici, on lance juste le son.
                Console.WriteLine("[Audio] Thème musical démarré.");
            }

            // 2. Cache des chemins pour les bruitages
            _laserPath = Path.Combine(soundsDir, "tir_laser.wav");
            _flamePath = Path.Combine(soundsDir, "lance_flamme.wav");

            Console.WriteLine("[Audio] Systèmes de bruitages prêts.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Audio] Erreur d'initialisation sonore : {ex.Message}");
        }
    }

    public static void StopTheme()
    {
        if (_themePlayer.Playing) _themePlayer.Stop();
    }

    private static string? GetSoundsDirectory()
    {
        string[] possibleDirs = {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "UI.Desktop", "Assets", "Sounds"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Sounds")
        };

        foreach (var dir in possibleDirs)
        {
            if (Directory.Exists(dir)) return dir;
        }
        return null;
    }

    public static void PlayPiou()
    {
        if (string.IsNullOrEmpty(_laserPath)) return;

        lock (_lock)
        {
            if ((DateTime.Now - _lastLaser).TotalMilliseconds < 100) return; // Plus de délai car pas de pool
            _lastLaser = DateTime.Now;
            
            try { _laserPlayer.Play(_laserPath); } catch { }
        }
    }

    public static void PlayFlamme()
    {
        if (string.IsNullOrEmpty(_flamePath)) return;

        lock (_lock)
        {
            if ((DateTime.Now - _lastFlame).TotalMilliseconds < 150) return;
            _lastFlame = DateTime.Now;
            
            try { _flamePlayer.Play(_flamePath); } catch { }
        }
    }
}
