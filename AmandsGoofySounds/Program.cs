using BepInEx;
using BepInEx.Configuration;
using EFT;
using System.Reflection;
using Aki.Reflection.Patching;
using UnityEngine;
using System.Threading.Tasks;

namespace AmandsGoofySounds
{
    [BepInPlugin("com.Amanda.GoofySounds", "GoofySounds", "1.0.1")]
    public class AmandsGoofySoundsPlugin : BaseUnityPlugin
    {
        public static GameObject Hook;
        public static AmandsGoofySoundsClass AmandsGoofySoundsClassComponent;
        public static ConfigEntry<bool> EnableSounds { get; set; }
        public static ConfigEntry<float> Distance { get; set; }
        public static ConfigEntry<int> Rolloff { get; set; }
        public static ConfigEntry<float> Volume { get; set; }
        public static ConfigEntry<float> RandomChance { get; set; }
        public static ConfigEntry<float> MinRandom { get; set; }
        public static ConfigEntry<float> MaxRandom { get; set; }
        public static ConfigEntry<float> HitChance { get; set; }
        public static ConfigEntry<float> DeathChance { get; set; }
        public static ConfigEntry<float> SpottedChance { get; set; }
        public static ConfigEntry<float> SpottedCooldown { get; set; }
        private void Awake()
        {
            Hook = new GameObject();
            AmandsGoofySoundsClassComponent = Hook.AddComponent<AmandsGoofySoundsClass>();
            DontDestroyOnLoad(Hook);
        }
        private void Start()
        {
            EnableSounds = Config.Bind("AmandsGoofySounds", "EnableSounds", true, new ConfigDescription("Supported Files WAV OGG", null, new ConfigurationManagerAttributes { Order = 210 }));
            Distance = Config.Bind("AmandsGoofySounds", "Distance", 99f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 200 }));
            Rolloff = Config.Bind("AmandsGoofySounds", "Rolloff", 100, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 190 }));
            Volume = Config.Bind("AmandsGoofySounds", "Volume", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 4f), new ConfigurationManagerAttributes { Order = 180 }));
            RandomChance = Config.Bind("AmandsGoofySounds", "RandomChance", 0.69f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes { Order = 170 }));
            MinRandom = Config.Bind("AmandsGoofySounds", "MinRandomTimer", 10f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 160 }));
            MaxRandom = Config.Bind("AmandsGoofySounds", "MaxRandomTimer", 60f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 150 }));
            HitChance = Config.Bind("AmandsGoofySounds", "HitChance", 0.69f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes { Order = 140 }));
            DeathChance = Config.Bind("AmandsGoofySounds", "DeathChance", 0.69f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes { Order = 130 }));
            SpottedChance = Config.Bind("AmandsGoofySounds", "SpottedChance", 0.69f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 1f), new ConfigurationManagerAttributes { Order = 120 }));
            SpottedCooldown = Config.Bind("AmandsGoofySounds", "SpottedCooldown", 30.0f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110 }));

            new AmandsLocalPlayerPatch().Enable();
            new AmandsGoofySoundsKillPatch().Enable();
            new AmandsGoofySoundsDamagePatch().Enable();
            new AmandsGoofySoundsGoalPatch().Enable();
        }
    }
    public class AmandsLocalPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocalPlayer).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref Task<LocalPlayer> __result)
        {
            LocalPlayer localPlayer = __result.Result;
            if (localPlayer != null && localPlayer.IsYourPlayer)
            {
                AmandsGoofySoundsClass.localPlayer = localPlayer;
                AmandsGoofySoundsPlugin.AmandsGoofySoundsClassComponent.PlaySoundRandom();
            }
        }
    }
    public class AmandsGoofySoundsKillPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnBeenKilledByAggressor", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref Player __instance, Player aggressor, DamageInfo damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            if (UnityEngine.Random.Range(0.0f, 0.99f) < AmandsGoofySoundsPlugin.DeathChance.Value && !__instance.IsYourPlayer) AmandsGoofySoundsPlugin.AmandsGoofySoundsClassComponent.PlaySoundDeath(damageInfo.HitPoint);
        }
    }
    public class AmandsGoofySoundsDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("ApplyDamageInfo", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostFix(ref Player __instance, DamageInfo damageInfo, EBodyPart bodyPartType)
        {
            if (UnityEngine.Random.Range(0.0f, 0.99f) < AmandsGoofySoundsPlugin.HitChance.Value && !__instance.IsYourPlayer) AmandsGoofySoundsPlugin.AmandsGoofySoundsClassComponent.PlayAmandsGoofySounds(ESoundType.Hit,__instance.ProfileId,__instance.Position,__instance.Transform.Original);
        }
    }
    public class AmandsGoofySoundsGoalPatch : ModulePatch
    {
        public static float TimeTest = 0;
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotGroupClass).GetMethod("CalcGoalForBot");
        }
        [PatchPostfix]
        public static void PatchPostfix(BotOwner bot)
        {
            object goalEnemy = bot.Memory.GetType().GetProperty("GoalEnemy").GetValue(bot.Memory);

            if (goalEnemy != null)
            {
                IAIDetails person = (IAIDetails)goalEnemy.GetType().GetProperty("Person").GetValue(goalEnemy);
                bool isVisible = (bool)goalEnemy.GetType().GetProperty("IsVisible").GetValue(goalEnemy);

                if (person.IsYourPlayer && isVisible)
                {
                    if (TimeTest < Time.time)
                    {
                        TimeTest = Time.time + AmandsGoofySoundsPlugin.SpottedCooldown.Value;
                        if (UnityEngine.Random.Range(0.0f, 0.99f) < AmandsGoofySoundsPlugin.SpottedChance.Value) AmandsGoofySoundsPlugin.AmandsGoofySoundsClassComponent.PlayAmandsGoofySounds(ESoundType.Spotted, bot.GetPlayer.ProfileId, bot.GetPlayer.Position, bot.GetPlayer.Transform.Original) ;
                    }
                }
            }
        }
    }
}
