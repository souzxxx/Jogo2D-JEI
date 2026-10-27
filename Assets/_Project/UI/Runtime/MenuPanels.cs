using AFirma.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>Janelas de Opções e Créditos (menu principal, esconderijo e pausa do golpe).</summary>
    static class MenuPanels
    {
        /// <summary>Volumes, tela cheia e apagar o save (dupla confirmação).</summary>
        public static void Options(VisualElement anyElement) => Ui.Modal(anyElement, "Opções", body =>
        {
            foreach (var (channel, label) in new[] { ("master", "Geral"), ("music", "Música"), ("sfx", "Efeitos") })
            {
                var slider = new Slider(label, 0f, 1f) { name = "volume-" + channel, value = Services.Audio.GetVolume(channel) };
                slider.RegisterValueChangedCallback(e => Services.Audio.SetVolume(channel, e.newValue));
                body.Add(slider);
            }
            var fullscreen = new Toggle("Tela cheia") { name = "fullscreen", value = Screen.fullScreen };
            fullscreen.RegisterValueChangedCallback(e => Screen.fullScreen = e.newValue);
            body.Add(fullscreen);

            var erase = Ui.Btn("Apagar save", () =>
                Ui.Confirm(body, "Apagar o save?", "Some tudo: Cr$, crew, golpes e Fama.", "Apagar", () =>
                {
                    Services.StartNewGame();    // o estado em memória também volta do zero
                    Services.Save.DeleteSave(); // e leva o backup junto
                    SceneFlow.GoToMainMenu();
                }, twice: true), "btn--red");
            erase.name = "delete-save";
            body.Add(erase);
        });

        /// <summary>Lê Resources/Creditos.txt (a mesma lista do README); linha com "# " vira título.</summary>
        public static void Credits(VisualElement anyElement) => Ui.Modal(anyElement, "Créditos", body =>
        {
            var list = new ScrollView();
            list.AddToClassList("credits");
            foreach (var line in Resources.Load<TextAsset>("Creditos").text.Split('\n'))
            {
                if (line.StartsWith("# ")) list.Add(Ui.Text(line.Substring(2).Trim(), "h2"));
                else if (line.Trim().Length > 0) list.Add(Ui.Text(line.Trim(), "dialog__body"));
            }
            body.Add(list);
        });
    }
}
