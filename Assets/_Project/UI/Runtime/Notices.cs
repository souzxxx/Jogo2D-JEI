using System;
using System.Collections.Generic;
using AFirma.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>
    /// Avisos do jogo: bips da Dona Cida (pager no canto, um por vez), toasts dos Notice e o popup
    /// "Enquanto você tava fora…". Escuta desde o boot, então nada se perde em troca de cena; cada tela chama Attach.
    /// </summary>
    static class Notices
    {
        static readonly Queue<(string sender, string message)> bips = new Queue<(string, string)>();
        static readonly Queue<string> notices = new Queue<string>();
        static (double money, double seconds)? offline;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Listen()
        {
            bips.Clear();
            notices.Clear();
            offline = null;
            GameEvents.Bip += (sender, message) => bips.Enqueue((sender, message));
            GameEvents.Notice += notices.Enqueue;
            // Bicos e golpes no automático avisam cada um o seu: o popup soma os dois.
            GameEvents.OfflineEarnings += (money, seconds) => offline = (money + (offline?.money ?? 0), Math.Max(seconds, offline?.seconds ?? 0));
            GameEvents.NewGameStarted += () => { bips.Clear(); offline = null; }; // o que estava na fila era do jogo anterior
        }

        /// <summary>
        /// Mostra o que chegar enquanto a tela de root estiver aberta. Cada bip fica 5 s, ou até clicar.
        /// toastsOnly (menu principal): bips e o popup offline esperam o esconderijo, pra não tampar os botões.
        /// </summary>
        public static void Attach(VisualElement root, bool toastsOnly = false)
        {
            var screen = root.Q(className: "screen");
            Button pager = null;
            (string sender, string message) showing = default;
            float until = 0f;
            screen.schedule.Execute(() =>
            {
                while (notices.Count > 0) Ui.Toast(screen, notices.Dequeue());
                if (toastsOnly) return;
                if (offline is { } away)
                {
                    offline = null;
                    var time = TimeSpan.FromSeconds(away.seconds);
                    Ui.Modal(screen, "Enquanto você tava fora…", body => body.Add(Ui.Text(
                        $"A firma rendeu {Formulas.Money(away.money)} em " +
                        (time.TotalHours >= 1 ? $"{(int)time.TotalHours} h {time.Minutes} min." : $"{Math.Max(1, time.Minutes)} min."),
                        "dialog__body")));
                }

                // O jornal do golpe ocupa o meio da tela e o pager do canto cairia em cima da capa (em tela estreita não
                // cabem os dois lado a lado): o bip espera o jornal fechar e aparece no esconderijo, sem perder a vez.
                if (root.Q("news") != null)
                {
                    if (pager != null)
                    {
                        Requeue(showing);
                        pager.RemoveFromHierarchy();
                        pager = null;
                    }
                    return;
                }

                if (pager != null && Time.unscaledTime < until) return;
                pager?.RemoveFromHierarchy();
                pager = null;
                if (bips.Count == 0) return;
                showing = bips.Dequeue();
                pager = new Button { name = "pager" }.OnClick(() => until = 0f);
                pager.AddToClassList("pager");
                pager.Add(Ui.Text("BIP · " + showing.sender, "pager__from"));
                pager.Add(Ui.Text(showing.message, "pager__text"));
                screen.Add(pager);
                UiSounds.Play(UiSounds.Get?.bip);
                until = Time.unscaledTime + 5f;
            }).Every(200);
        }

        /// <summary>Volta o bip pra frente da fila (ele ainda não foi lido).</summary>
        static void Requeue((string sender, string message) bip)
        {
            var rest = bips.ToArray();
            bips.Clear();
            bips.Enqueue(bip);
            foreach (var b in rest) bips.Enqueue(b);
        }
    }
}
