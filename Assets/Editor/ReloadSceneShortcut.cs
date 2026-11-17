using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KindFall.EditorTools
{
    /// <summary>
    /// Açık olan sahneyi ve varlıkları tek tıkla veya kısayolla diskten yeniden yükleyen Editör aracı.
    /// </summary>
    public static class ReloadSceneShortcut
    {
        /// <summary>
        /// AssetDatabase'i yeniler ve aktif sahneyi diskten yeniden yükler (Kısayol: Ctrl+Shift+R / Cmd+Shift+R).
        /// </summary>
        [MenuItem("KindFall/Reload Active Scene %#r")]
        public static void ReloadActiveScene()
        {
            if (Application.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            AssetDatabase.Refresh();

            Scene activeScene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                Debug.LogWarning("[KindFall] Aktif sahne henüz kaydedilmediği için diskten yüklenemedi.");
                return;
            }

            EditorSceneManager.OpenScene(activeScene.path, OpenSceneMode.Single);
            Debug.Log($"[KindFall] Sahne yenilendi: {activeScene.path}");
        }
    }
}
