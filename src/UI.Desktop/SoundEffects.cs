using System;
using System.IO;
using System.Runtime.InteropServices;
#if WINDOWS
using System.Windows.Media;
#endif
using System.Collections.Generic;

namespace TowerFluffy.UI.Desktop;

public static class SoundEffects
{
    private static DateTime _lastLaser = DateTime.MinValue;
    private static DateTime _lastFlame = DateTime.MinValue;
    private static readonly object _lock = new();
    
#if WINDOWS
    private static readonly List<MediaPlayer> _laserPool = new();
    private static int _nextLaserIndex = 0;

    private static readonly List<MediaPlayer> _flamePool = new();
    private static int _nextFlameIndex = 0;

    private const int PoolSize = 10;

    private static MediaPlayer? _themePlayer;
#endif

    public static void Initialize()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                Console.WriteLine("[Audio] Initialisation du système sonore...");
                
                string? soundsDir = GetSoundsDirectory();
                if (soundsDir == null)
                {
                    Console.WriteLine("[Audio] ERREUR : Impossible de localiser le dossier Assets/Sounds.");
                    return;
                }

#if WINDOWS
                // 1. Initialisation du thème musical
                string themePath = Path.Combine(soundsDir, "theme_futur.mp3");
                if (File.Exists(themePath))
                {
                    _themePlayer = new MediaPlayer();
                    _themePlayer.Open(new Uri(themePath));
                    _themePlayer.Volume = 0.3;
                    
                    // On s'abonne à MediaOpened pour lancer la lecture dès que c'est prêt (instantané)
                    _themePlayer.MediaOpened += (s, e) => {
                        _themePlayer.Play();
                        Console.WriteLine("[Audio] Thème musical démarré.");
                    };

                    // Boucle de lecture
                    _themePlayer.MediaEnded += (s, e) => { 
                        _themePlayer.Position = TimeSpan.FromMilliseconds(1); // Un léger offset peut aider pour la boucle
                        _themePlayer.Play(); 
                    };
                    
                    Console.WriteLine($"[Audio] Fichier thème détecté : {themePath}");
                }
#endif
                else
                {
                    Console.WriteLine($"[Audio] ATTENTION : Thème non trouvé à {themePath}");
                }

#if WINDOWS
                // 2. Initialisation du pool de bruitages (Laser)
                string laserPath = Path.Combine(soundsDir, "tir_laser.wav");
                if (File.Exists(laserPath))
                {
                    var uri = new Uri(laserPath);
                    for (int i = 0; i < PoolSize; i++)
                    {
                        var player = new MediaPlayer();
                        player.Open(uri);
                        player.Volume = 0.5;
                        _laserPool.Add(player);
                    }
                    Console.WriteLine($"[Audio] Pool Laser prêt ({PoolSize} lecteurs).");
                }

                // 3. Initialisation du pool de bruitages (Flamme)
                string flamePath = Path.Combine(soundsDir, "lance_flamme.wav");
                if (File.Exists(flamePath))
                {
                    var uri = new Uri(flamePath);
                    for (int i = 0; i < PoolSize; i++)
                    {
                        var player = new MediaPlayer();
                        player.Open(uri);
                        player.Volume = 0.4;
                        _flamePool.Add(player);
                    }
                    Console.WriteLine($"[Audio] Pool Flamme prêt ({PoolSize} lecteurs).");
                }
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Audio] Erreur d'initialisation sonore : {ex.Message}");
            }
        }
    }

    public static void StopTheme()
    {
#if WINDOWS
        if (_themePlayer != null)
        {
            _themePlayer.Dispatcher.BeginInvoke(() => {
                _themePlayer.Stop();
            });
        }
#endif
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
#if WINDOWS
        if (_laserPool.Count == 0) return;

        lock (_lock)
        {
            if ((DateTime.Now - _lastLaser).TotalMilliseconds < 40) return;
            _lastLaser = DateTime.Now;
            
            var player = _laserPool[_nextLaserIndex];
            _nextLaserIndex = (_nextLaserIndex + 1) % PoolSize;

            player.Dispatcher.BeginInvoke(() => {
                player.Stop();
                player.Position = TimeSpan.Zero;
                player.Play();
            });
        }
#endif
    }

    public static void PlayFlamme()
    {
#if WINDOWS
        if (_flamePool.Count == 0) return;

        lock (_lock)
        {
            if ((DateTime.Now - _lastFlame).TotalMilliseconds < 30) return;
            _lastFlame = DateTime.Now;
            
            var player = _flamePool[_nextFlameIndex];
            _nextFlameIndex = (_nextFlameIndex + 1) % PoolSize;

            player.Dispatcher.BeginInvoke(() => {
                player.Stop();
                player.Position = TimeSpan.Zero;
                player.Play();
            });
        }
#endif
    }
}
