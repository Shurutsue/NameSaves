using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.IO;
using BepInEx.Logging;

namespace NameSaves
{
    [BepInPlugin(Plugin.GUID, Plugin.NAME, Plugin.VERSION)]
    [BepInProcess("KoboldKare.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "Shurutsue.SaveNames";
        public const string NAME = "Save Names";
        public const string VERSION = "1.0.0";

        public static ManualLogSource logger;

        private static Harmony harmony = new(GUID);
        private void Awake()
        {
            logger = Logger;
            // Plugin startup logic
            Logger.LogInfo($"Plugin {Plugin.GUID} is loaded!");
            harmony.PatchAll();
        }
    }

    public class SaveUISetName : MonoBehaviour
    {
        public Rect Window;
        public static string FileName = "";
        private SaveUIDisplay Instance;
        public bool Visible = false;

        public void Initialize(SaveUIDisplay __instance)
        {
            Instance = __instance;
        }

        public void Awake()
        {
            DontDestroyOnLoad(this);
            Window = new(0, 0, 100, 50);
        }

        public void OnGUI()
        {
            if (!Visible || Event.current.type != EventType.Layout)
                return;

            GUI.backgroundColor = Color.black;
            Window = new Rect(Screen.width / 2 - Window.width / 2, Screen.height / 2 - Window.height / 2, Window.width, Window.height);
            Window = GUILayout.Window(0, Window, new GUI.WindowFunction(RenderUI), $"Save Game", GUI.skin.window);
        }

        private void SaveBTN()
        {
            Instance.gameObject.transform.parent.gameObject.SetActive(true);

            string saveExtension = typeof(SaveManager).GetField("saveExtension", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as string;
            string imageExtension = typeof(SaveManager).GetField("imageExtension", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as string;
            string saveDataPath = typeof(SaveManager).GetProperty("saveDataPath", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as string;
            string saveFilePath = $"{saveDataPath}{FileName}{saveExtension}";
            string imageFilePath = $"{saveDataPath}{FileName}{imageExtension}";
            
            if (!string.IsNullOrEmpty(FileName)) {
                if (File.Exists(saveFilePath))
                {
                    Plugin.logger.LogInfo("Deleting old save.");
                    File.Delete(saveFilePath);
                }
                if (File.Exists(imageFilePath))
                {
                    Plugin.logger.LogInfo("Deleting old save image.");
                    File.Delete(imageFilePath);
                }
            }

            Visible = false;
            typeof(SaveUIDisplay).GetMethod("AddNewSave", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Instance, new object[] { FileName });
        }

        private void RenderUI(int i)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Save name:", GUILayout.Width(100f));
            FileName = GUILayout.TextField(FileName, GUILayout.Width(300f));
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Save"))
                SaveBTN();
        }
    }

    [HarmonyPatch(typeof(SaveManager))]
    internal static class SaveManagerPatches
    {
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Load)), HarmonyPostfix]
        public static void LoadPrefix(string filename) {
            SaveUISetName.FileName = Path.GetFileNameWithoutExtension(filename);
        }
    }

    [HarmonyPatch(typeof(SaveUIDisplay))]
    internal static class SavePatches
    {
        [HarmonyPatch("Awake"), HarmonyPrefix]
        public static bool prefix(SaveUIDisplay __instance)
        {
            GameObject old = GameObject.Find(Plugin.GUID + "_UISetName");
            if (old != null)
                GameObject.DestroyImmediate(old);
            GameObject SetName = new GameObject(Plugin.GUID + "_UISetName");
            SaveUISetName component = SetName.AddComponent<SaveUISetName>();

            component.Initialize(__instance);

            Traverse.Create(__instance).Field<Button>("createNewSaveButton").Value.onClick.AddListener(() => {
                component.Visible = true;
                __instance.gameObject.transform.parent.gameObject.SetActive(false);
                
            });

            return false;
        }
    }
}
