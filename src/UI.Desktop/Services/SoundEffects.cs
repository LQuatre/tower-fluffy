using System;
using System.IO;
using NAudio.Wave;

namespace TowerFluffy.UI.Desktop.Services;

public static class SoundEffects
{
    private static WaveOutEvent? _themePlayer;
    private static AudioFileReader? _themeReader;
    private static string? _themePath;

    public static void Initialize()
    {
        var baseDir = AppContext.BaseDirectory;
        _themePath = Path.Combine(baseDir, "Assets", "Sounds", "music.wav");
        
        if (!File.Exists(_themePath))
        {
            Console.WriteLine($"[Audio] Erreur : Fichier introuvable {_themePath}");
        }
    }

    public static void PlayTheme()
    {
        if (_themePath != null && File.Exists(_themePath))
        {
            try 
            {
                if (_themePlayer == null)
                {
                    _themeReader = new AudioFileReader(_themePath);
                    _themePlayer = new WaveOutEvent();
                    _themePlayer.Init(_themeReader);
                    _themePlayer.PlaybackStopped += (s, e) => {
                        if (_themeReader != null && _themePlayer != null)
                        {
                            _themeReader.Position = 0;
                            _themePlayer.Play();
                        }
                    };
                }
                _themePlayer.Play();
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"[Audio] Erreur de lecture du thème : {ex.Message}");
            }
        }
    }

    public static void StopTheme()
    {
        _themePlayer?.Stop();
        _themePlayer?.Dispose();
        _themePlayer = null;
        _themeReader?.Dispose();
        _themeReader = null;
    }

    private static void PlayEffect(string filename)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds", filename);
        if (File.Exists(path))
        {
            try
            {
                var reader = new AudioFileReader(path);
                if (filename == "lance_flamme.wav")
                {
                    reader.Volume = 0.75f; // Baissé de 25%
                }
                
                var player = new WaveOutEvent();
                player.Init(reader);
                player.PlaybackStopped += (s, e) => {
                    player.Dispose();
                    reader.Dispose();
                };
                player.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Audio] Erreur d'effet {filename} : {ex.Message}");
            }
        }
    }

    public static void PlayLaser() => PlayEffect("tir_laser.wav");
    public static void PlayFlame() => PlayEffect("lance_flamme.wav");
    public static void PlayOrbital() => PlayEffect("laser_orbital.wav");
    public static void PlayCannon() => PlayEffect("canon.wav");
}
