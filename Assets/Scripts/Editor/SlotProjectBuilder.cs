using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlotGame.Editor
{
    /// <summary>
    /// One-click (or command-line) generator for everything that is tedious to author by hand:
    /// sprite import settings and slicing, ScriptableObject data, fonts, animation clips and
    /// controllers, prefabs and the main scene.
    /// <para>
    /// The generated assets are committed to the repository, so the project opens ready to play;
    /// the builder exists so the setup is reproducible and reviewable as code rather than as a
    /// long list of manual Inspector steps.
    /// </para>
    /// Menu: <b>Slot Game ▸ Rebuild Project Assets</b>.
    /// CLI: <c>Unity -batchmode -quit -projectPath . -executeMethod SlotGame.Editor.SlotProjectBuilder.BuildFromCommandLine</c>
    /// </summary>
    public static partial class SlotProjectBuilder
    {
        internal const string ScenePath = "Assets/Scenes/SlotMachine.unity";
        internal const string DataFolder = "Assets/Data";
        internal const string PrefabFolder = "Assets/Prefabs";
        internal const string AnimationFolder = "Assets/Animations";
        internal const string SymbolArtFolder = "Assets/Art/Sprites/Symbols";
        internal const string MachineArtFolder = "Assets/Art/Sprites/Machine";
        internal const string EffectsArtFolder = "Assets/Art/Sprites/Effects";
        internal const string UiSpriteFolder = "Assets/UI/Sprites";
        internal const string FontFolder = "Assets/UI/Fonts";
        internal const string SoundFolder = "Assets/Sounds";

        [MenuItem("Slot Game/Rebuild Project Assets", priority = 0)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Slot Game", "Importing TextMesh Pro resources", 0.05f);
                ImportTextMeshProEssentials();

                EditorUtility.DisplayProgressBar("Slot Game", "Configuring sprites", 0.15f);
                ConfigureSprites();

                EditorUtility.DisplayProgressBar("Slot Game", "Configuring audio", 0.25f);
                ConfigureAudio();

                EditorUtility.DisplayProgressBar("Slot Game", "Creating fonts", 0.35f);
                CreateFonts();

                EditorUtility.DisplayProgressBar("Slot Game", "Creating game data", 0.45f);
                GameData data = CreateGameData();

                EditorUtility.DisplayProgressBar("Slot Game", "Creating animations", 0.6f);
                AnimationSet animations = CreateAnimations();

                EditorUtility.DisplayProgressBar("Slot Game", "Building scene and prefabs", 0.75f);
                BuildScene(data, ReloadFonts(), animations);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[SlotProjectBuilder] Project assets rebuilt.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>Batch-mode entry point.</summary>
        public static void BuildFromCommandLine()
        {
            BuildAll();
        }

        internal static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetFolder);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>Creates the asset, or overwrites the existing one in place so GUID references survive.</summary>
        internal static T CreateOrReplaceAsset<T>(T asset, string path) where T : Object
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }

            EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(asset);
            return existing;
        }
    }
}
