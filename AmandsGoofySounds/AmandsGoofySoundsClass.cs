using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using EFT;
using Comfort.Common;

namespace AmandsGoofySounds
{
    public class AmandsGoofySoundsClass : MonoBehaviour
    {
        public static LocalPlayer localPlayer;
        private static List<AudioClip> SoundRandom = new List<AudioClip>();
        private static List<AudioClip> SoundHit = new List<AudioClip>();
        private static List<AudioClip> SoundDeath = new List<AudioClip>();
        private static List<AudioClip> SoundSpotted = new List<AudioClip>();
        private static Dictionary<string, float> Playing = new Dictionary<string, float>();
        public void Start()
        {
            ReloadFiles();
        }
        public void Update()
        {
        }
        public async void PlaySoundRandom()
        {
            await Task.Delay((int)(UnityEngine.Random.Range(AmandsGoofySoundsPlugin.MinRandom.Value, AmandsGoofySoundsPlugin.MaxRandom.Value) * 1000));
            if (localPlayer != null)
            {
                List<IPlayer> SoundPlayers = new List<IPlayer>();
                foreach (IPlayer AIDetails in Singleton<GameWorld>.Instance.RegisteredPlayers)
                {
                    if (AIDetails.IsYourPlayer) continue;
                    if (Vector3.Distance(localPlayer.Position, AIDetails.Position) < AmandsGoofySoundsPlugin.Distance.Value) SoundPlayers.Add(AIDetails);
                }
                if (SoundPlayers.Count != 0 && (UnityEngine.Random.Range(0.0f, 0.99f) < AmandsGoofySoundsPlugin.RandomChance.Value))
                {
                    System.Random rnd = new System.Random();
                    IPlayer AIDetails = SoundPlayers[rnd.Next(SoundPlayers.Count)];
                    PlayAmandsGoofySounds(ESoundType.Random,AIDetails.ProfileId,AIDetails.Position,AIDetails.Transform.Original);
                }
                PlaySoundRandom();
            }
        }
        public void PlayAmandsGoofySounds(ESoundType soundType, string ProfileId, Vector3 Position, Transform Original)
        {
            if (localPlayer != null)
            {
                if (Vector3.Distance(localPlayer.Position, Position) > AmandsGoofySoundsPlugin.Distance.Value) return;
                if (Playing.ContainsKey(ProfileId))
                {
                    if (Playing[ProfileId] < Time.time)
                    {
                        Playing.Remove(ProfileId);
                    }
                    else
                    {
                        return;
                    }
                }
                if (AmandsGoofySoundsPlugin.EnableSounds.Value)
                {
                    System.Random rnd = new System.Random();
                    AudioClip audioClip = null;
                    switch (soundType)
                    {
                        case ESoundType.Random:
                            audioClip = SoundRandom[rnd.Next(SoundRandom.Count)];
                            break;
                        case ESoundType.Hit:
                            audioClip = SoundHit[rnd.Next(SoundHit.Count)];
                            break;
                        case ESoundType.Spotted:
                            audioClip = SoundSpotted[rnd.Next(SoundSpotted.Count)];
                            break;
                    }
                    if (audioClip != null)
                    {
                        Singleton<BetterAudio>.Instance.PlayAtPoint(Position, audioClip, AmandsGoofySoundsPlugin.Distance.Value, BetterAudio.AudioSourceGroupType.Character, AmandsGoofySoundsPlugin.Rolloff.Value, AmandsGoofySoundsPlugin.Volume.Value, EOcclusionTest.Regular).transform.SetParent(Original);
                        Playing.Add(ProfileId, Time.time + audioClip.length);
                    }
                }
            }
        }
        public void PlaySoundDeath(Vector3 position)
        {
            if (AmandsGoofySoundsPlugin.EnableSounds.Value && localPlayer != null)
            {
                if (Vector3.Distance(localPlayer.Position, position) > AmandsGoofySoundsPlugin.Distance.Value) return;
                System.Random rnd = new System.Random();
                AudioClip audioClip = SoundDeath[rnd.Next(SoundDeath.Count)];
                if (audioClip != null)
                {
                    Singleton<BetterAudio>.Instance.PlayAtPoint(position, audioClip, AmandsGoofySoundsPlugin.Distance.Value, BetterAudio.AudioSourceGroupType.Character, AmandsGoofySoundsPlugin.Rolloff.Value, AmandsGoofySoundsPlugin.Volume.Value, EOcclusionTest.Regular);
                }
            }
        }
        public void ReloadFiles()
        {
            SoundRandom.Clear();
            SoundHit.Clear();
            SoundDeath.Clear();
            SoundSpotted.Clear();
            string[] AudioFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/GoofySounds/Random/");
            foreach (string File in AudioFiles)
            {
                LoadAudioClip(File, ESoundType.Random);
            }
            AudioFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/GoofySounds/Hit/");
            foreach (string File in AudioFiles)
            {
                LoadAudioClip(File, ESoundType.Hit);
            }
            AudioFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/GoofySounds/Death/");
            foreach (string File in AudioFiles)
            {
                LoadAudioClip(File, ESoundType.Death);
            }
            AudioFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/GoofySounds/Spotted/");
            foreach (string File in AudioFiles)
            {
                LoadAudioClip(File, ESoundType.Spotted);
            }
        }
        async void LoadAudioClip(string path, ESoundType soundType)
        {
            AudioClip audioClip = await RequestAudioClip(path);
            switch (soundType)
            {
                case ESoundType.Random:
                    SoundRandom.Add(audioClip);
                    break;
                case ESoundType.Hit:
                    SoundHit.Add(audioClip);
                    break;
                case ESoundType.Death:
                    SoundDeath.Add(audioClip);
                    break;
                case ESoundType.Spotted:
                    SoundSpotted.Add(audioClip);
                    break;
            }
        }
        async Task<AudioClip> RequestAudioClip(string path)
        {
            string extension = Path.GetExtension(path);
            AudioType audioType = AudioType.WAV;
            switch (extension)
            {
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
            }
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            var SendWeb = www.SendWebRequest();

            while (!SendWeb.isDone)
                await Task.Yield();

            if (www.isNetworkError || www.isHttpError)
            {
                return null;
            }
            else
            {
                AudioClip audioclip = DownloadHandlerAudioClip.GetContent(www);
                return audioclip;
            }
        }
    }
    public enum ESoundType
    {
        Random,
        Hit,
        Death,
        Spotted
    }
}
