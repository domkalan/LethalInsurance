using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace LethalInsurance
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("us.domkalan.lethalextendedterminal")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "us.domkalan.lethalinsurance";
        public const string NAME = "Lethal Insurance";
        public const string VERSION = "1.1.0";

        public static Plugin instance;
        private void Awake()
        {
            Plugin.instance = this;
            
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            
            // Hook plugin
            LethalInsurance.Patches.LethalInsurance.Register();
            
            // NetCode Weaver
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            
            // Hook into game using harmony
            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        
        public void Log(LogType type, string contents)
        {
            Logger.LogInfo(contents);
        }
    }
}
