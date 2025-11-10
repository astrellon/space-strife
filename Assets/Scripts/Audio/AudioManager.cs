using System.Linq;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using AYellowpaper.SerializedCollections;

#nullable enable

namespace Orbits
{
    public class AudioManager : MonoBehaviour
    {
        [System.Serializable]
        public class BGMEntry
        {
            public AssetReference? Reference;
            public bool BuiltinLoop = false;
            public float TestStart = 0.0f;
            public float LoopStart = 0.0f;
            public float LoopEnd = 0.0f;
            [DefaultValue(1.0f)]
            public float MaxVolume = 1.0f;
            public bool IgnoreHighpass = false;

            public AsyncOperationHandle<AudioClip>? LoadHandle { get; private set; }

            public AsyncOperationHandle<AudioClip> StartLoading()
            {
                if (this.LoadHandle.HasValue)
                {
                    return this.LoadHandle.Value;
                }

                var handle = Addressables.LoadAssetAsync<AudioClip>(this.Reference);
                this.LoadHandle = handle;
                return handle;
            }
        }

        public class BGMKeyValuePair
        {
            public static readonly BGMKeyValuePair Empty = new("", null);

            public readonly string Id = "";
            public readonly BGMEntry? Entry;

            public BGMKeyValuePair(string id, BGMEntry? entry)
            {
                this.Id = string.IsNullOrWhiteSpace(id) ? "<Empty>" : id;
                this.Entry = entry;
            }
        }

        #region Fields
        public static AudioManager? Instance;
        public AudioSourcePools? Pools;

        [Header("UI One Offs")]
        public AudioOneOffs? UIHover;
        public AudioOneOffs? UIClick;
        public AudioOneOffs? UIConfirmation;

        public AudioSource UIPrefab;
        public AudioSource DialoguePrefab;
        public AudioSource GlobalSFXPrefab;

        public AudioMixerGroup MasterGroup;

        public SerializedDictionary<string, BGMEntry> MapBGM = new(System.StringComparer.OrdinalIgnoreCase);
        private float audioMixerPitch = 1.0f;

        public BGMAudio BGMSource;
        private BGMKeyValuePair currentBGM = BGMKeyValuePair.Empty;
        private BGMKeyValuePair nextBGM = BGMKeyValuePair.Empty;
        private bool switchBGM = false;
        private LerpFloat fadeoutBGM = new();
        public float highpassAmount = 0.0f;
        private float maxHighapssAmount = 5000.0f;
        #endregion

        #region Unity Methods
        void Awake()
        {
            Instance = this;
            GameOptions.OnChange += this.OnSettingsChange;
        }

        void Start()
        {
            this.SetMasterVolume(GameOptions.MasterVolume);
            this.SetMusicVolume(GameOptions.MusicVolume);
            this.SetSFXVolume(GameOptions.SFXVolume);
            this.SetUIVolume(GameOptions.UIVolume);
            this.SetDialogueVolume(GameOptions.DialogueVolume);
        }

        void Update()
        {
            var newAudioPitch = 1.0f - (1.0f - GameManager.Instance.TimeScaleRatio) * 0.5f;
            if (newAudioPitch != this.audioMixerPitch)
            {
                this.MasterGroup.audioMixer.SetFloat("sfxPitch", newAudioPitch);
                this.audioMixerPitch = newAudioPitch;
            }

            if (this.currentBGM.Entry != null)
            {
                if (this.currentBGM.Entry.IgnoreHighpass)
                {
                    this.highpassAmount = 0.0f;
                }
                else
                {
                    var dt = GameManager.Instance.GamePaused ? 1.0f : -1.0f;
                    dt *= Time.unscaledDeltaTime * Mathf.PI;
                    this.highpassAmount = Mathf.Clamp01(this.highpassAmount + dt);
                }
            }

            if (this.highpassAmount > 0.0f)
            {
                this.MasterGroup.audioMixer.SetFloat("highpassWet", 0.0f);
                this.MasterGroup.audioMixer.SetFloat("highpassCutoff", this.highpassAmount * this.maxHighapssAmount);
            }
            else
            {
                this.MasterGroup.audioMixer.SetFloat("highpassWet", -80.0f);
            }

            if (this.switchBGM)
            {
                this.fadeoutBGM.Update(Time.unscaledDeltaTime);
                if (this.fadeoutBGM.IsComplete)
                {
                    this.switchBGM = false;
                    this.BGMSource.AudioSource.Stop();

                    if (this.nextBGM.Entry != null)
                    {
                        this.currentBGM = this.nextBGM;
                        this.nextBGM = BGMKeyValuePair.Empty;

                        Debug.Log($"Start loading BGM: {this.currentBGM.Id}");
                        this.BGMSource.AudioSource.volume = this.currentBGM.Entry.MaxVolume;
                        var handle = this.currentBGM.Entry.StartLoading();
                        if (handle.IsDone)
                        {
                            Debug.Log($"BGM already loaded");
                            this.OnBGMLoaded(handle);
                        }
                        else
                        {
                            Debug.Log($"BGM not yet loaded, waiting for load");
                            handle.Completed += this.OnBGMLoaded;
                        }
                    }
                    else
                    {
                        Debug.Log("Stopping BGM");
                    }
                }
                else
                {
                    var volume = CalculateDBFromLinear(this.fadeoutBGM.Value);
                    this.BGMSource.AudioSource.volume = volume;
                }
            }
        }

        private void OnBGMLoaded(AsyncOperationHandle<AudioClip> handle)
        {
            handle.WaitForCompletion();

            // If the current entry is unset or if the handle that loaded is no-longer the current one, ignore.
            if (this.currentBGM.Entry == null || !this.currentBGM.Entry.LoadHandle.Equals(handle))
            {
                return;
            }

            Debug.Log($"BGM Loaded");
            this.BGMSource.InitFromEntry(handle.Result, this.currentBGM.Entry);
            this.BGMSource.AudioSource.Play();

            handle.Completed -= this.OnBGMLoaded;
        }

        void OnDestroy()
        {
            GameOptions.OnChange -= this.OnSettingsChange;
        }
        #endregion

        #region Methods
        public bool TryGetBGM(string id, out BGMKeyValuePair result)
        {
            if (this.MapBGM.TryGetValue(id, out var found))
            {
                result = new(id, found);
                return true;
            }

            result = new(id, null);
            return false;
        }

        public void SetBGM(string id, float fadeOutTime = 1.0f)
        {
            this.TryGetBGM(id, out var result);
            this.SetBGM(result, fadeOutTime);
        }

        public void SetBGM(BGMKeyValuePair entry, float fadeOutTime = 1.0f)
        {
            if (entry.Entry == this.currentBGM.Entry)
            {
                return;
            }

            if (this.currentBGM.Entry == null)
            {
                this.fadeoutBGM = LerpFloat.Empty();
            }
            else if (!this.switchBGM)
            {
                this.fadeoutBGM = new() { From = this.BGMSource.AudioSource.volume, To = 0.0f, Speed = fadeOutTime };
            }

            Debug.Log($"Changing BGM to: {entry.Id}");

            this.nextBGM = entry;
            this.switchBGM = true;

            if (entry.Entry != null)
            {
                entry.Entry.StartLoading();
            }
        }

        public static void PlayOneOffDialogue(AudioOneOffs? clipSource, Vector3 position)
        {
            if (Instance != null)
            {
                Instance.PlayOneOff(clipSource, Instance.DialoguePrefab, position);
            }
            else
            {
                Debug.LogWarning("No AudioManager");
            }
        }

        public static void PlayOneOffUI(AudioOneOffs? clipSource, Vector3 position)
        {
            if (Instance != null)
            {
                Instance.PlayOneOff(clipSource, Instance.UIPrefab, position);
            }
            else
            {
                Debug.LogWarning("No AudioManager");
            }
        }

        public static void PlayOneOffGlobal(AudioOneOffs? clipSource, AudioSource? sourcePrefab, Vector3 position)
        {
            if (Instance != null)
            {
                Instance.PlayOneOff(clipSource, sourcePrefab, position);
            }
            else
            {
                Debug.LogWarning("No AudioManager");
            }
        }

        public static void PlayOneOffGlobal(AudioClip? clip, AudioSource? sourcePrefab, Vector3 position, float randomisePitch)
        {
            if (Instance != null)
            {
                Instance.PlayOneOff(clip, sourcePrefab, position, randomisePitch);
            }
            else
            {
                Debug.LogWarning("No AudioManager");
            }
        }

        public void PlayOneOff(AudioOneOffs? clipSource, AudioSource? sourcePrefab, Vector3 position)
        {
            if (clipSource != null)
            {
                this.PlayOneOff(clipSource.GetRandom(), sourcePrefab, position, clipSource.RandomisePitch);
            }
        }

        public void PlayOneOff(AudioClip? clip, AudioSource? sourcePrefab, Vector3 position, float randomisePitch)
        {
            if (clip == null || this.Pools == null || sourcePrefab == null)
            {
                return;
            }

            var audioSource = this.Pools.Spawn(sourcePrefab);
            audioSource.transform.position = position;
            audioSource.pitch = 1.0f + Random.Range(-randomisePitch, randomisePitch);
            audioSource.PlayOneShot(clip);

            var sourceReturn = audioSource.gameObject.GetOrAddComponent<AudioSourceReturn>();
            sourceReturn.Source = audioSource;
            sourceReturn.OriginalPrefab = sourcePrefab;
        }

        /// <summary>
        /// Sets the master volume mixer group.
        /// This uses the a linear input from [0, 1] and will logarithmically turn it into [-80, 0] dB
        /// </summary>
        public void SetMasterVolume(float value)
        {
            var db = CalculateDBFromLinear(value);
            this.MasterGroup.audioMixer.SetFloat("volume", db);
        }

        public void SetMusicVolume(float value)
        {
            var db = CalculateDBFromLinear(value);
            this.MasterGroup.audioMixer.SetFloat("bgmVolume", db);
        }

        public void SetSFXVolume(float value)
        {
            var db = CalculateDBFromLinear(value);
            this.MasterGroup.audioMixer.SetFloat("sfxVolume", db);
        }

        public void SetUIVolume(float value)
        {
            var db = CalculateDBFromLinear(value);
            this.MasterGroup.audioMixer.SetFloat("uiVolume", db);
        }

        public void SetDialogueVolume(float value)
        {
            var db = CalculateDBFromLinear(value);
            this.MasterGroup.audioMixer.SetFloat("dialogueVolume", db);
        }

        public static float CalculateLogFromLinear(float input)
        {
            var logValue = ((float)System.Math.Log10(input + 0.1f) + 1.0f) / 1.0413926851582251f; // 1.041... is the result of Math.Log10(1.1) + 1, to normalise 0 -> 1
            return logValue;
        }

        public static float CalculateDBFromLinear(float input)
        {
            var logValue = CalculateLogFromLinear(input);
            return logValue * 80.0f - 80.0f;
        }

        private void OnSettingsChange(SettingType type)
        {
            if (type == SettingType.MasterVolume)
            {
                this.SetMasterVolume(GameOptions.MasterVolume);
            }
            else if (type == SettingType.MusicVolume)
            {
                this.SetMusicVolume(GameOptions.MusicVolume);
            }
            else if (type == SettingType.SFXVolume)
            {
                this.SetSFXVolume(GameOptions.SFXVolume);
            }
            else if (type == SettingType.UIVolume)
            {
                this.SetUIVolume(GameOptions.UIVolume);
            }
            else if (type == SettingType.DialogueVolume)
            {
                this.SetDialogueVolume(GameOptions.DialogueVolume);
            }
        }
        #endregion
    }
}