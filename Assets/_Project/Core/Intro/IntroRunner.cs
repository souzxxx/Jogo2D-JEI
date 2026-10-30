using UnityEngine;
using UnityEngine.SceneManagement;

namespace AFirma.Core
{
    /// <summary>Faz o diretor andar (bip, fim, troca de cena) e desenha o letreiro e o Pular provisórios.</summary>
    class IntroRunner : MonoBehaviour
    {
        void OnEnable() => SceneManager.activeSceneChanged += OnSceneChanged;
        void OnDisable() => SceneManager.activeSceneChanged -= OnSceneChanged;

        static void OnSceneChanged(Scene previous, Scene next) => IntroDirector.OnSceneChanged(next);

        void Update() => IntroDirector.Update();

        void OnGUI()
        {
            var intro = IntroDirector.Current;
            if (intro == null) return;

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                e.Use();
                IntroDirector.Skip();
                return;
            }
            if (!IntroDirector.ShowPlaceholderOverlay) return;

            if (intro.Caption != null)
            {
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(18, Screen.height / 24), wordWrap = true };
                GUI.Label(new Rect(Screen.width * 0.1f, 0, Screen.width * 0.8f, Screen.height), intro.Caption, style);
            }
            if (GUI.Button(new Rect(Screen.width - 130, Screen.height - 50, 120, 40), "Pular ▸")) IntroDirector.Skip();
        }
    }
}
