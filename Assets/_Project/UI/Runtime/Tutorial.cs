using AFirma.Core;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>
    /// Primeiro golpe guiado: cada passo é um bip da Dona Cida e um brilho no que clicar (mapa → planejar → ir →
    /// alerta → porta; o evento do Seu Zé vem do golpe). Acaba quando o evento tutorial é resolvido (tutorialDone)
    /// ou no "Pular tutorial" do esconderijo. Dicas de primeira vez valem sempre, uma vez por save (seenTips "dica:").
    /// </summary>
    static class Tutorial
    {
        public static bool Active => Services.State != null && !Services.State.tutorialDone && !IntroDirector.IsPlaying;

        public static void Step(string id, VisualElement target, string text)
        {
            if (!Active || target == null || !Tip(id, text)) return;
            target.AddToClassList("glow");
            // pisca enquanto tiver "glow": some no clique, em 15 s ou no Pular
            target.schedule.Execute(() => target.EnableInClassList("glow--on", target.ClassListContains("glow") && !target.ClassListContains("glow--on"))).Every(400);
            target.RegisterCallback<PointerDownEvent>(_ => target.RemoveFromClassList("glow"), TrickleDown.TrickleDown);
            target.schedule.Execute(() => target.RemoveFromClassList("glow")).StartingIn(15000);
        }

        /// <summary>Bip da Dona Cida na primeira vez (por save). Devolve se mandou.</summary>
        public static bool Tip(string id, string text)
        {
            var seen = Services.State?.seenTips;
            if (seen == null || seen.Contains("dica:" + id) || IntroDirector.IsPlaying) return false;
            seen.Add("dica:" + id);
            GameEvents.RaiseBip(Services.Config != null ? Services.Config.mentorName : "Dona Cida", text);
            return true;
        }

        public static void Skip(VisualElement root)
        {
            Services.State.tutorialDone = true;
            root.Query(className: "glow").ForEach(e => e.RemoveFromClassList("glow"));
        }
    }
}
