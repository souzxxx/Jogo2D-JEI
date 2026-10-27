using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AFirma.UI
{
    /// <summary>
    /// Atalhos pra montar e ligar as telas (UI Toolkit). Os estilos ficam em Styles/Firma.uss.
    /// Regra de UX (ESCOPO 7.2): botão indisponível continua clicável e diz o motivo; ação irreversível pede confirmação.
    /// </summary>
    public static class Ui
    {
        /// <summary>Toca o som de clique e chama a ação. Hover também tem som.</summary>
        public static Button OnClick(this Button button, Action action)
        {
            button.clicked += () =>
            {
                UiSounds.Click();
                action();
            };
            button.RegisterCallback<PointerEnterEvent>(_ => UiSounds.Hover());
            return button;
        }

        public static Button OnClick(this VisualElement root, string name, Action action) => root.Q<Button>(name).OnClick(action);

        /// <summary>Marca o botão como indisponível (cinza) sem desabilitar: o clique explica o motivo.</summary>
        public static void SetAvailable(this Button button, bool available) => button.EnableInClassList("btn--off", !available);

        public static void SetShown(this VisualElement element, bool shown) => element.EnableInClassList("hidden", !shown);

        public static VisualElement El(string classes = null, params VisualElement[] children)
        {
            var element = new VisualElement();
            AddClasses(element, classes);
            foreach (var child in children) element.Add(child);
            return element;
        }

        public static Label Text(string text, string classes = null)
        {
            var label = new Label(text);
            AddClasses(label, classes);
            return label;
        }

        public static Button Btn(string text, Action onClick, string classes = null)
        {
            var button = new Button { text = text };
            AddClasses(button, "btn " + classes);
            return button.OnClick(onClick);
        }

        public static VisualElement Image(Sprite sprite, string classes)
        {
            var image = El(classes);
            if (sprite != null) image.style.backgroundImage = new StyleBackground(sprite);
            return image;
        }

        /// <summary>Aviso que some sozinho, embaixo da tela (a tela precisa ter um #toasts).</summary>
        public static void Toast(VisualElement anyElement, string text)
        {
            var toasts = anyElement.panel?.visualTree.Q("toasts");
            if (toasts == null || string.IsNullOrEmpty(text)) return;
            var toast = Text(text, "dark toast");
            toasts.Add(toast);
            toast.schedule.Execute(() => toast.style.opacity = 0f).StartingIn(3000);
            toast.schedule.Execute(toast.RemoveFromHierarchy).StartingIn(3500);
        }

        /// <summary>
        /// Popup de confirmação por cima da tela de anyElement (o .screen dela).
        /// Com twice = true pergunta de novo antes de fazer.
        /// </summary>
        public static void Confirm(VisualElement anyElement, string title, string body, string yes, Action onYes, bool twice = false)
        {
            var screen = anyElement.panel?.visualTree.Q(className: "screen") ?? anyElement;
            VisualElement scrim = null;
            scrim = El("scrim",
                El("panel dialog",
                    Text(title, "h2"),
                    Text(body, "dialog__body"),
                    El("row dialog__buttons",
                        Btn("Cancelar", () => scrim.RemoveFromHierarchy()),
                        Btn(yes, () =>
                        {
                            scrim.RemoveFromHierarchy();
                            if (twice) Confirm(screen, "Certeza mesmo?", "Não tem volta.", yes, onYes);
                            else { UiSounds.Stamp(); onYes(); }
                        }, "btn--red"))));
            screen.Add(scrim);
        }

        /// <summary>Janela por cima da tela de anyElement com título, conteúdo e Fechar (opções, créditos, menu).</summary>
        public static VisualElement Modal(VisualElement anyElement, string title, Action<VisualElement> build)
        {
            var screen = anyElement.panel?.visualTree.Q(className: "screen") ?? anyElement;
            VisualElement scrim = null;
            var body = El("dialog__content");
            scrim = El("scrim", El("panel dialog dialog--wide", Text(title, "h2"), body,
                El("row dialog__buttons", Btn("Fechar", () => scrim.RemoveFromHierarchy()))));
            build(body);
            screen.Add(scrim);
            UiSounds.Paper();
            return scrim;
        }

        /// <summary>
        /// Painel que acompanha o jogo enquanto está na tela. build monta tudo e é chamado de novo quando key() muda
        /// (alguém foi preso, subiu de nível...); os updates que o build registra (relógio, botão disponível)
        /// rodam 4 vezes por segundo sem remontar, pra não piscar nem perder o hover.
        /// </summary>
        public static VisualElement Live(Func<string> key, Action<VisualElement, List<Action>> build)
        {
            var panel = El("grow");
            var updates = new List<Action>();
            string last = null;
            void Refresh()
            {
                var now = key();
                if (now != last)
                {
                    last = now;
                    panel.Clear();
                    updates.Clear();
                    build(panel, updates);
                }
                foreach (var update in updates) update();
            }
            Refresh();
            panel.schedule.Execute(Refresh).Every(250);
            return panel;
        }

        static void AddClasses(VisualElement element, string classes)
        {
            if (string.IsNullOrEmpty(classes)) return;
            foreach (var c in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) element.AddToClassList(c);
        }
    }
}
