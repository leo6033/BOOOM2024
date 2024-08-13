using System.Collections;
using Engine.Runtime;
using Engine.SettingModule;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Gameplay
{
    public class SoundModule: UnitySingleton<SoundModule>
    {
        public AudioSource bgmSource;
        [FormerlySerializedAs("meowSource")] public AudioSource elimateSource;
        public AudioSource otherSource;

        public void PlayBGM(int resourceId)
        {
            var table = TableModule.Get("SoundsResources");
            var path = table.GetData((uint)resourceId, "SoundsResource");

            var audio = Resources.Load<AudioClip>(path.ToString());
            bgmSource.clip = audio;
            bgmSource.Play();
        }

        public void PlayAudio(int resourceId)
        {
            var table = TableModule.Get("SoundsResources");
            var path = table.GetData((uint)resourceId, "SoundsResource");

            var audio = Resources.Load<AudioClip>(path.ToString());
            otherSource.clip = audio;
            otherSource.Play();
        }

        public void PlayElimate()
        {
            var resourceId = Random.Range(1, 3);
            
            var table = TableModule.Get("SoundsResources");
            var path = table.GetData((uint)resourceId, "SoundsResource");

            var audio = Resources.Load<AudioClip>(path.ToString());
            elimateSource.clip = audio;
            elimateSource.Play();
        }
    }
}