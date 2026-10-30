using AFirma.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>Menu principal (cena MainMenu): Continuar, Novo jogo (com a abertura), Ver abertura, Opções, Créditos e Sair (só fora do navegador).</summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuScreen : MonoBehaviour
    {
        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Q(className: "screen").FadeIn();
            var continueButton = root.OnClick("continue", SceneFlow.GoToHideout);
            continueButton.SetShown(Services.Save.HasSave);
            root.OnClick("new-game", () =>
            {
                if (Services.Save.HasSave)
                    Ui.Confirm(root, "Novo jogo?", "O save atual vai pro lixo: Cr$, crew e golpes.", "Começar do zero", NewGame);
                else NewGame();
            });
            root.OnClick("replay-intro", IntroDirector.Replay);
            root.OnClick("options", () => MenuPanels.Options(root));
            root.OnClick("credits", () => MenuPanels.Credits(root));
#if UNITY_WEBGL
            root.Q<Button>("quit").SetShown(false);
#else
            root.OnClick("quit", Application.Quit);
#endif
            Notices.Attach(root, toastsOnly: true);
            Services.Audio.PlayMusic(MusicTrack.Menu);
        }

        static void NewGame() => IntroDirector.NewGameWithIntro(); // abertura e depois o esconderijo
    }
}
