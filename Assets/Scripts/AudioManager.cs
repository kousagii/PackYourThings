using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip blockPlaceClip;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void PlayButtonClick()
    {
        if (sfxSource != null && buttonClickClip != null)
        {
            sfxSource.PlayOneShot(buttonClickClip);
        }
    }

    public void PlayBlockPlace()
    {
        if (sfxSource != null && blockPlaceClip != null)
        {
            sfxSource.PlayOneShot(blockPlaceClip);
        }
    }
}
