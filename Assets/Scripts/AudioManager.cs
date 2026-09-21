using UnityEngine.Audio;
using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioMixer mainMixer;
    public Sound[] sounds;

    public static AudioManager AM;

    void Awake()
    {
        if (AM == null)
        {
            AM = this;
            DontDestroyOnLoad(gameObject);
            InitializeSounds();
        }
        else if (AM != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void InitializeSounds()
    {
        if (sounds == null) return;

        foreach (var s in sounds)
        {
            if (s == null) continue;
            if (s.source == null)
            {
                s.source = gameObject.AddComponent<AudioSource>();
            }
            s.source.clip = s.clip;
            s.source.volume = s.valume;
            s.source.pitch = s.pitch;
            s.source.outputAudioMixerGroup = s.mixerGroup;
            s.source.loop = s.loop;
        }
    }

    public void Play(string name)
    {
        if (AM != null && AM != this)
        {
            AM.Play(name);
            return;
        }

        if (sounds == null) return;

        Sound s = Array.Find(sounds, sound => sound != null && sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Nie ma takiego dźwięku jak: " + name);
            return;
        }

        if (s.source == null)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.valume;
            s.source.pitch = s.pitch;
            s.source.outputAudioMixerGroup = s.mixerGroup;
            s.source.loop = s.loop;
        }

        if (s.source != null && s.clip != null)
        {
            s.source.Play();
        }
    }

    public void Stop(string name)
    {
        if (AM != null && AM != this)
        {
            AM.Stop(name);
            return;
        }

        if (sounds == null) return;

        Sound s = Array.Find(sounds, sound => sound != null && sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Nie ma takiego dźwięku jak: " + name);
            return;
        }

        if (s.source != null)
        {
            s.source.Stop();
        }
    }
}
