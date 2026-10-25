using UnityEngine;

/// <summary>Единая точка сохранения и применения настроек экрана.</summary>
public static class ScreenSettings
{
    private const string ResolutionKey = "screen_resolution";
    private const string FullscreenKey = "screen_fullscreen";
    private const string FpsKey = "screen_fps";

    public static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080)
    };

    public static readonly string[] FpsNames = { "30 FPS", "60 FPS", "120 FPS", "ВЕРТ. СИНХР." };

    public static int ResolutionIndex
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, DefaultResolutionIndex()), 0, Resolutions.Length - 1); }
    }

    public static bool Fullscreen
    {
        get { return PlayerPrefs.GetInt(FullscreenKey, 0) == 1; }
    }

    public static int FpsMode
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(FpsKey, 1), 0, FpsNames.Length - 1); }
    }

    public static void ApplySaved()
    {
        int index = ResolutionIndex;
        FullScreenMode mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(Resolutions[index].x, Resolutions[index].y, mode);
        ApplyFps(FpsMode);
    }

    public static void SetResolution(int index)
    {
        index = Mathf.Clamp(index, 0, Resolutions.Length - 1);
        PlayerPrefs.SetInt(ResolutionKey, index);
        PlayerPrefs.Save();
        FullScreenMode mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(Resolutions[index].x, Resolutions[index].y, mode);
    }

    public static void SetFullscreen(bool fullscreen)
    {
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
        Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }

    public static void SetFps(int mode)
    {
        mode = Mathf.Clamp(mode, 0, FpsNames.Length - 1);
        PlayerPrefs.SetInt(FpsKey, mode);
        PlayerPrefs.Save();
        ApplyFps(mode);
    }

    public static void ApplyFps(int mode)
    {
        // Не смешиваем VSync и ручной лимит: иначе один из них молча игнорируется.
        if (mode == 3)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            return;
        }
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = mode == 0 ? 30 : mode == 1 ? 60 : 120;
    }

    public static int DefaultResolutionIndex()
    {
        int width = Mathf.Max(1280, Display.main.systemWidth);
        int height = Mathf.Max(720, Display.main.systemHeight - 80);
        for (int i = Resolutions.Length - 1; i >= 0; i--)
        {
            if (Resolutions[i].x <= width && Resolutions[i].y <= height) return i;
        }
        return 0;
    }

    public static string ResolutionName(int index)
    {
        index = Mathf.Clamp(index, 0, Resolutions.Length - 1);
        return Resolutions[index].x + "×" + Resolutions[index].y;
    }
}
