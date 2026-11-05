using System.Collections.Generic;
using System.Linq;
using AFirma.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>
    /// Faixa Golpes rolando do esconderijo (ESCOPO 6.11), embaixo das abas: um cartão por golpe no automático com a
    /// miniatura do prédio (giroflex na sirene), a sacola, o alerta, os rostos da crew e as salas como bolinhas.
    /// No fim de cada volta sai a mini manchete e o "+Cr$" voa pro caixa; quem foi em cana fica 5 s no cartão.
    /// Clicar no cartão abre Repetir e Chamar de volta.
    /// </summary>
    static class AutoHeistStrip
    {
        /// <summary>O golpe no automático em que o ladrão está (null se não estiver em nenhum).</summary>
        public static AutoHeistRun RunOf(ThiefState thief) => Services.AutoHeist.Running.FirstOrDefault(r => r.crew.Contains(thief));

        public static void Attach(VisualElement root)
        {
            var strip = root.Q("auto-heists");
            var screen = root.Q(className: "screen");
            var header = Ui.Text("Golpes rolando", "light auto__head");
            var empty = Ui.Text("Ninguém na rua. Golpe com o cofre limpo vai pro automático no Planejar.", "dark auto__empty");
            strip.Add(header);
            strip.Add(empty);
            var cards = new Dictionary<AutoHeistRun, VisualElement>();
            var jailedUntil = new Dictionary<AutoHeistRun, float>();

            void OnFinished(AutoHeistRun run, HeistResult result)
            {
                if (strip.panel == null) { GameEvents.AutoHeistFinished -= OnFinished; return; } // a tela fechou
                if (!cards.TryGetValue(run, out var card)) return;
                Headline(screen, card, run, result);
                if (result.moneyGained > 0) Fly(screen, card, root.Q("money"), "+" + Formulas.Money(result.moneyGained));
                if (result.arrestedThiefIds.Count == 0) return;
                jailedUntil[run] = Time.unscaledTime + 5f;
                var names = Services.Crew.Crew.Where(t => result.arrestedThiefIds.Contains(t.id)).Select(t => t.nickname);
                card.Add(Ui.Text("EM CANA: " + string.Join(", ", names), "stamp auto__jail"));
            }
            GameEvents.AutoHeistFinished += OnFinished;

            strip.schedule.Execute(() =>
            {
                var running = Services.AutoHeist.Running;
                foreach (var run in cards.Keys.ToList())
                    if (!running.Contains(run) && !(jailedUntil.TryGetValue(run, out var until) && Time.unscaledTime < until))
                    {
                        cards[run].RemoveFromHierarchy();
                        cards.Remove(run);
                        jailedUntil.Remove(run);
                    }
                foreach (var run in running)
                    if (!cards.ContainsKey(run)) strip.Add(cards[run] = Card(run));
                foreach (var pair in cards) Refresh(pair.Key, pair.Value);

                empty.SetShown(cards.Count == 0);
                strip.SetShown(cards.Count > 0 || Services.Progression.AllHeists.Any(h => h != null && Services.AutoHeist.IsAutoUnlocked(h)));
            }).Every(100);
        }

        static VisualElement Card(AutoHeistRun run)
        {
            var card = new Button();
            card.AddToClassList("auto__card");
            card.name = "auto-" + run.heist.id;
            var thumb = Ui.Image(run.heist.thumbnail, "auto__thumb");
            if (run.heist.thumbnail == null) thumb.Add(Ui.El("pin__dot")); // sem miniatura: o pino do mapa
            var faces = Ui.El("row");
            foreach (var thief in run.crew)
                faces.Add(Ui.Image(Services.Db.GetThiefType(thief.type)?.portrait, "auto__face"));
            var dots = Ui.El("row auto__dots");
            for (int i = 0; i < run.RoomCount; i++) dots.Add(Ui.El("auto__dot"));
            card.Add(Ui.El("row", thumb, Ui.El("grow",
                Ui.Text("", "auto__bag"),
                Ui.El("auto__bar", Ui.El("auto__fill")),
                faces)));
            card.Add(dots);
            return card.OnClick(() => Controls(card, run));
        }

        /// <summary>Sacola, alerta, sala atual piscando e o giroflex (borda e miniatura alternando) na sirene.</summary>
        static void Refresh(AutoHeistRun run, VisualElement card)
        {
            bool blink = (int)(Time.unscaledTime * 4f) % 2 == 0;
            card.EnableInClassList("auto__card--red", run.siren && blink);
            card.EnableInClassList("auto__card--blue", run.siren && !blink);
            var sprite = run.siren && blink && run.heist.thumbnailSiren != null ? run.heist.thumbnailSiren : run.heist.thumbnail;
            if (sprite != null) card.Q(className: "auto__thumb").style.backgroundImage = new StyleBackground(sprite);
            card.Q<Label>(className: "auto__bag").text = Formulas.Money(run.bag);
            var fill = card.Q(className: "auto__fill");
            fill.style.width = Length.Percent(run.alert);
            fill.EnableInClassList("auto__fill--high", run.alert >= 70f || run.siren);
            int i = 0;
            card.Query(className: "auto__dot").ForEach(dot =>
            {
                dot.EnableInClassList("auto__dot--done", i < run.roomIndex);
                dot.EnableInClassList("auto__dot--now", i == run.roomIndex && blink);
                i++;
            });
        }

        static void Controls(VisualElement card, AutoHeistRun run)
        {
            VisualElement scrim = null;
            scrim = Ui.Modal(card, run.heist.displayName + " no automático", body =>
            {
                body.Add(Ui.Text($"{(run.order == AutoHeistOrder.Quiet ? "Na moita" : "Até o cofre")} · {run.runsDone} voltas · trouxe {Formulas.Money(run.moneyEarned)}", "dialog__body"));
                body.Add(Ui.Text("Crew: " + string.Join(", ", run.crew.Select(t => t.DisplayName)), "dialog__body"));
                Button repeat = null;
                repeat = Ui.Btn(RepeatText(run.repeat), () =>
                {
                    Services.AutoHeist.SetRepeat(run, !run.repeat);
                    repeat.text = RepeatText(run.repeat);
                });
                repeat.name = "auto-repeat-toggle";
                var recall = Ui.Btn("Chamar de volta (mete o pé agora)", () =>
                {
                    Services.AutoHeist.Recall(run);
                    scrim.RemoveFromHierarchy();
                }, "btn--red");
                recall.name = "auto-recall";
                body.Add(Ui.El("row dialog__buttons", repeat, recall));
            });
        }

        public static string RepeatText(bool repeat) => repeat ? "Repetir: ligado" : "Repetir: desligado";

        /// <summary>Mini capa do jornal ao lado do cartão, por 4 s.</summary>
        static void Headline(VisualElement screen, VisualElement card, AutoHeistRun run, HeistResult result)
        {
            var text = Services.Db.headlines != null ? Services.Db.headlines.Pick(result, run.heist.displayName, new System.Random()) : run.heist.displayName;
            var news = Ui.El("auto__news", Ui.Text("O GUANABARENSE", "auto__mast"), Ui.Text(text));
            news.style.top = card.worldBound.y - screen.worldBound.y;
            screen.Add(news.FadeIn());
            news.schedule.Execute(() => news.AddToClassList("fade")).StartingIn(4000);
            news.schedule.Execute(news.RemoveFromHierarchy).StartingIn(4400);
        }

        /// <summary>"+Cr$" que sai do cartão e voa até o caixa.</summary>
        static void Fly(VisualElement screen, VisualElement from, VisualElement to, string text)
        {
            var label = Ui.Text(text, "auto__fly");
            label.style.left = from.worldBound.x - screen.worldBound.x;
            label.style.top = from.worldBound.y - screen.worldBound.y;
            screen.Add(label);
            var delta = to.worldBound.position - from.worldBound.position;
            label.schedule.Execute(() => label.style.translate = new Translate(delta.x, delta.y)).StartingIn(20);
            label.schedule.Execute(label.RemoveFromHierarchy).StartingIn(900);
        }
    }
}
