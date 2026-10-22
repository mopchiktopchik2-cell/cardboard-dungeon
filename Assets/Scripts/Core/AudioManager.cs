using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private AudioSource music;
    private readonly List<AudioSource> sources = new List<AudioSource>();
    private int sourceCursor;
    private Coroutine musicFade;

    public float Master = 0.8f;
    public float MusicVolume = 0.6f;
    public float SfxVolume = 0.85f;

    public void Init()
    {
        foreach (var clip in Resources.LoadAll<AudioClip>("Audio"))
        {
            clips[clip.name] = clip;
        }

        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;
        music.volume = 0f;

        for (int i = 0; i < 12; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            sources.Add(s);
        }

        Master = PlayerPrefs.GetFloat("vol_master", 0.8f);
        MusicVolume = PlayerPrefs.GetFloat("vol_music", 0.6f);
        SfxVolume = PlayerPrefs.GetFloat("vol_sfx", 0.85f);
        Debug.Log("[Cardboard] Audio loaded: " + clips.Count + " clips");
    }

    public void SetVolumes(float master, float musicVol, float sfxVol)
    {
        Master = master;
        MusicVolume = musicVol;
        SfxVolume = sfxVol;
        PlayerPrefs.SetFloat("vol_master", master);
        PlayerPrefs.SetFloat("vol_music", musicVol);
        PlayerPrefs.SetFloat("vol_sfx", sfxVol);
        PlayerPrefs.Save();
        if (music != null) music.volume = 0.42f * Master * MusicVolume;
    }

    public void ApplyVolumes()
    {
        SetVolumes(Master, MusicVolume, SfxVolume);
    }

    public void Play(string name, float volume = 1f, float pitch = 1f, float pitchJitter = 0.04f)
    {
        if (!clips.TryGetValue(name, out var clip) || clip == null) return;
        var src = sources[sourceCursor++ % sources.Count];
        src.pitch = pitch + Random.Range(-pitchJitter, pitchJitter);
        src.volume = volume * SfxVolume * Master;
        src.PlayOneShot(clip, 1f);
    }

    public void Music(string name, float fade = 1f)
    {
        if (!clips.TryGetValue(name, out var clip) || clip == null) return;
        if (music.clip == clip && music.isPlaying) return;
        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(FadeMusic(clip, fade));
    }

    public void StopMusic(float fade = 0.6f)
    {
        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(FadeOut(fade));
    }

    private IEnumerator FadeMusic(AudioClip clip, float duration)
    {
        if (music.isPlaying)
        {
            float start = music.volume;
            float t = 0f;
            while (t < duration * 0.5f)
            {
                t += Time.unscaledDeltaTime;
                music.volume = Mathf.Lerp(start, 0f, t / (duration * 0.5f + 0.001f));
                yield return null;
            }
        }
        music.clip = clip;
        music.volume = 0f;
        music.Play();
        float target = 0.42f * Master * MusicVolume;
        float time = 0f;
        while (time < duration * 0.5f)
        {
            time += Time.unscaledDeltaTime;
            music.volume = Mathf.Lerp(0f, target, time / (duration * 0.5f + 0.001f));
            yield return null;
        }
        music.volume = target;
    }

    private IEnumerator FadeOut(float duration)
    {
        float start = music.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            music.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        music.Stop();
    }
}
