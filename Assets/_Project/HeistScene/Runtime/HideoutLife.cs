using System.Collections.Generic;
using System.Linq;
using System.Text;
using AFirma.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Esconderijo vivo (ESCOPO 9 e pedido do professor: "ver a firma crescendo e farmando"). Uma camada atrás da tela do
    /// esconderijo do Enzo, desenhada por cima do fundo do porão, na mesma escala (640x360):
    ///
    /// · os bicos passam nos monitores da prateleira (a cena de cada bico) e cada um solta "+Cr$" no ritmo da renda dele;
    ///   bico vazio fica com a cena apagada (VOLTO JÁ);
    /// · cada upgrade comprado vira um objeto no porão que cresce com o nível: Kombi, colchonetes, sapateira, painel de
    ///   ferramentas, diploma do advogado, telefone e agenda, máquinas de lavar dinheiro, rádio da polícia, neon do ponto
    ///   de bico e orelhão (que toca enquanto tem golpe no automático). Comprou: o objeto aparece com um pulo e "NOVO!"
    ///   assim que a aba fecha;
    /// · a crew disponível fica de bobeira no porão (anima, comemora e solta bordão); quem está no bico ou no golpe some;
    ///   quem está preso vira foto 3x4 com EM CANA no mural;
    /// · passar o mouse em qualquer coisa diz o que é (aprende jogando).
    ///
    /// Só lê os serviços e escuta os GameEvents; não muda nada no jogo.
    /// </summary>
    public class HideoutLife : MonoBehaviour
    {
        public HideoutLifeArt art;
        [Tooltip("De quantos em quantos segundos cada bico solta o seu \"+Cr$\".")]
        public float popEvery = 3f;

        const float W = 640f, H = 360f;
        static readonly float[] CrewSlots = { 150, 205, 260, 315, 372, 426, 530, 586 };
        // fotos 3x4 do mural (cantos de baixo, como no Esconderijo.png do Enzo)
        static readonly Vector2[] PhotoSlots = { new Vector2(396, 236), new Vector2(404, 182), new Vector2(206, 236), new Vector2(214, 172) };
        static readonly Color Mint = new Color(0.706f, 0.863f, 0.549f);
        static readonly Color Ink = new Color(0.055f, 0.047f, 0.082f);
        static readonly Color Cream = new Color(0.969f, 0.953f, 0.910f);

        class Monitor
        {
            public SideJobDef job;
            public VisualElement screen, bezel;
            public float x, popIn;
            public double income;
            public bool active;
        }

        class Gadget
        {
            public UpgradeEffect effect;
            public UpgradeDef def;
            public VisualElement root;
            public int level = -1;
            public bool revealPending;
            public float revealT = -1f;
        }

        class Mate
        {
            public ThiefState thief;
            public HideoutLifeArt.CrewLook look;
            public VisualElement body;
            public float phase, celebrateT = -1f;
            public float x;
        }

        class Floater
        {
            public VisualElement el;
            public float t, life, rise;
        }

        VisualElement hideout, layer, content;
        Label tip, bubble;
        readonly List<Monitor> monitors = new List<Monitor>();
        readonly Dictionary<UpgradeEffect, Gadget> gadgets = new Dictionary<UpgradeEffect, Gadget>();
        readonly List<Mate> mates = new List<Mate>();
        readonly List<VisualElement> photos = new List<VisualElement>();
        readonly List<Floater> floaters = new List<Floater>();
        VisualElement crewLayer, photoLayer;
        string crewSignature;
        float time, pollIn, chatterIn = 5f, bubbleLeft;
        bool firstRefresh = true;

        public VisualElement Layer => layer;

        void OnEnable()
        {
            GameEvents.UpgradePurchased += OnUpgradePurchased;
            GameEvents.GameLoaded += OnReset;
            GameEvents.NewGameStarted += OnReset;
            GameEvents.Prestiged += OnPrestiged;
            GameEvents.AutoHeistFinished += OnAutoHeistFinished;
        }

        void OnDisable()
        {
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
            GameEvents.GameLoaded -= OnReset;
            GameEvents.NewGameStarted -= OnReset;
            GameEvents.Prestiged -= OnPrestiged;
            GameEvents.AutoHeistFinished -= OnAutoHeistFinished;
            layer?.RemoveFromHierarchy();
            layer = null;
        }

        void OnUpgradePurchased(UpgradeDef def, int level) => RefreshGadgets();
        void OnReset() { firstRefresh = true; RefreshGadgets(); crewSignature = null; }
        void OnPrestiged(int fame, int wanted) => OnReset();

        void OnAutoHeistFinished(AutoHeistRun run, HeistResult result)
        {
            // a Kombi volta do golpe no automático com a grana
            if (layer == null || result == null || result.moneyGained <= 0) return;
            Float("+" + Formulas.Money(result.moneyGained), 64, 70, Mint, 18f, 2f);
        }

        // ---------------- Montagem ----------------

        bool TryBuild()
        {
            if (layer != null) return true;
            if (art == null || !Services.IsInitialized) return false;
            foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                var root = doc.rootVisualElement;
                hideout = root?.Q(className: "hideout");
                if (hideout != null) break;
            }
            if (hideout == null) return false;
            content = hideout.Q("content");

            layer = new VisualElement { name = "esconderijo-vivo", pickingMode = PickingMode.Ignore };
            layer.style.position = Position.Absolute;
            layer.style.left = layer.style.top = layer.style.right = layer.style.bottom = 0;
            hideout.Insert(0, layer);
            // os contêineres de layout da tela não seguram o mouse, pra dar pra passar o mouse nas coisas do porão
            foreach (var cls in new[] { "hideout__body", "hideout__tabs" })
                hideout.Query(className: cls).ForEach(e => e.pickingMode = PickingMode.Ignore);

            BuildMonitors();
            BuildGadgets();
            photoLayer = Group("mural");
            crewLayer = Group("crew");
            BuildTip();
            firstRefresh = true;
            RefreshGadgets();
            RefreshCrew(force: true);
            return true;
        }

        VisualElement Group(string name)
        {
            var g = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            g.style.position = Position.Absolute;
            g.style.left = g.style.top = g.style.right = g.style.bottom = 0;
            layer.Add(g);
            return g;
        }

        static void Place(VisualElement e, float x, float y, float w, float h)
        {
            e.style.position = Position.Absolute;
            e.style.left = Length.Percent(x / W * 100f);
            e.style.top = Length.Percent((H - y - h) / H * 100f);
            e.style.width = Length.Percent(w / W * 100f);
            e.style.height = Length.Percent(h / H * 100f);
        }

        static void SetSprite(VisualElement e, Sprite s)
        {
            e.style.backgroundImage = s != null ? new StyleBackground(s) : new StyleBackground(StyleKeyword.None);
        }

        static VisualElement Img(VisualElement parent, string name, Sprite s, float x, float y, float scale = 1f)
        {
            var e = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            float w = s != null ? s.rect.width * scale : 16f, h = s != null ? s.rect.height * scale : 16f;
            Place(e, x, y, w, h);
            SetSprite(e, s);
            parent.Add(e);
            return e;
        }

        void Hover(VisualElement e, System.Func<string> text)
        {
            e.pickingMode = PickingMode.Position;
            e.RegisterCallback<PointerEnterEvent>(ev => ShowTip(text(), ev.position));
            e.RegisterCallback<PointerMoveEvent>(ev => MoveTip(ev.position));
            e.RegisterCallback<PointerLeaveEvent>(_ => tip.style.display = DisplayStyle.None);
        }

        // ---------------- Monitores dos bicos ----------------

        void BuildMonitors()
        {
            var jobs = Services.Db.sideJobs.Where(j => j != null).ToList();
            int n = Mathf.Min(jobs.Count, 5);
            Img(layer, "prateleira", art.shelf, 128, 282);
            for (int i = 0; i < n; i++)
            {
                var job = jobs[i];
                var m = new Monitor { job = job, x = 134 + i * 66, popIn = popEvery * (0.3f + 0.17f * i) };
                m.screen = new VisualElement { name = "tela: " + job.id, pickingMode = PickingMode.Ignore };
                Place(m.screen, m.x + 4, 288 + 5, 48, 32);
                m.screen.style.backgroundColor = Ink;
                layer.Add(m.screen);
                m.bezel = Img(layer, "monitor: " + job.id, art.monitorOn, m.x, 288);
                Hover(m.bezel, () => MonitorTip(m));
                monitors.Add(m);
            }
        }

        string MonitorTip(Monitor m)
        {
            if (!m.job.requiresThief) return $"{m.job.displayName}\nSempre rolando: +{Formulas.Money(m.income)}/s";
            if (!m.active) return $"{m.job.displayName}\nVazio. Manda alguém pela aba Bicos.";
            return $"{m.job.displayName}\n+{Formulas.Money(m.income)}/s";
        }

        void UpdateMonitors(float dt, bool poll)
        {
            foreach (var m in monitors)
            {
                if (poll)
                {
                    if (!m.job.requiresThief)
                    {
                        m.active = true;
                        m.income = Services.Config != null ? Services.Config.scavengeIncomePerSecond : 0;
                    }
                    else
                    {
                        m.income = 0;
                        m.active = false;
                        foreach (var t in Services.Crew.Crew)
                            if (t.status == ThiefStatus.OnSideJob && t.sideJobId == m.job.id)
                            {
                                m.active = true;
                                m.income += Services.Economy.GetSideJobIncome(t, m.job);
                            }
                    }
                    SetSprite(m.bezel, m.active ? art.monitorOn : art.monitorOff);
                }

                if (m.job.HasScene)
                {
                    var frame = m.active ? m.job.SceneFrameAt(time) : (m.job.sceneEmpty != null ? m.job.sceneEmpty : m.job.SceneFrameAt(0f));
                    SetSprite(m.screen, frame);
                    m.screen.style.opacity = m.active || m.job.sceneEmpty != null ? 1f : 0.35f;
                }
                else
                {
                    // bico ainda sem cena: o ícone dele na tela, piscando de leve quando tem gente
                    SetSprite(m.screen, m.job.icon);
                    m.screen.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                    m.screen.style.opacity = m.active ? 0.75f + 0.25f * Mathf.Sin(time * 3f) : 0.35f;
                }

                if (!m.active || m.income <= 0) continue;
                m.popIn -= dt;
                if (m.popIn > 0f) continue;
                m.popIn += popEvery;
                // sai da tela da TV e sobe por cima dela (mais alto, a barra do caixa cobriria)
                Float("+" + Formulas.Money(Mathf.Max(1f, (float)(m.income * popEvery))), m.x + 28, 294, Mint, 18f, 1.5f);
            }
        }

        // ---------------- Upgrades viram coisas no porão ----------------

        void BuildGadgets()
        {
            foreach (UpgradeEffect effect in System.Enum.GetValues(typeof(UpgradeEffect)))
            {
                var def = Services.Db.upgrades.Find(u => u != null && u.effect == effect);
                if (def == null) continue;
                var g = new Gadget { effect = effect, def = def };
                g.root = new VisualElement { name = "upgrade: " + def.id, pickingMode = PickingMode.Ignore };
                g.root.style.position = Position.Absolute;
                g.root.style.left = g.root.style.top = g.root.style.right = g.root.style.bottom = 0;
                layer.Add(g.root);
                gadgets[effect] = g;
            }
        }

        void RefreshGadgets()
        {
            if (layer == null) return;
            foreach (var g in gadgets.Values)
            {
                int level = Services.Economy.GetUpgradeLevel(g.def);
                if (level == g.level) continue;
                bool grew = g.level >= 0 && level > g.level && !firstRefresh;
                g.level = level;
                g.root.Clear();
                if (level > 0) Draw(g);
                if (grew) g.revealPending = true;
            }
            firstRefresh = false;
        }

        VisualElement Part(Gadget g, string name, Sprite s, float x, float y)
        {
            var e = Img(g.root, name, s, x, y);
            Hover(e, () => $"{g.def.displayName} · nível {g.level}/{g.def.maxLevel}\n{g.def.description}");
            return e;
        }

        void Draw(Gadget g)
        {
            int lv = g.level;
            switch (g.effect)
            {
                case UpgradeEffect.HeistSlots:
                    Part(g, "Kombi", HideoutLifeArt.ByLevel(art.kombi, lv), 4, 2);
                    break;
                case UpgradeEffect.CrewSlots:
                    var bags = new[] { (172f, 52f), (204f, 52f), (236f, 52f), (268f, 52f), (188f, 66f), (220f, 66f), (252f, 66f) };
                    for (int i = 0; i < Mathf.Min(lv, bags.Length); i++)
                        Part(g, "Colchonete", art.sleepingBags[i % art.sleepingBags.Length], bags[i].Item1, bags[i].Item2);
                    break;
                case UpgradeEffect.SkillPercent:
                    Part(g, "Painel de ferramentas", art.pegboard, 478, 150);
                    for (int i = 0; i < Mathf.Min(lv, art.tools.Length); i++)
                        Part(g, "Ferramenta", art.tools[i], 482 + (i % 4) * 15, 222 - (i / 4) * 32);
                    break;
                case UpgradeEffect.StealthPercent:
                    Part(g, "Sapateira", HideoutLifeArt.ByLevel(art.shoeRacks, Mathf.CeilToInt(lv * art.shoeRacks.Length / (float)Mathf.Max(1, g.def.maxLevel))), 440, 4);
                    break;
                case UpgradeEffect.LawyerPercent:
                    Part(g, "Diploma", HideoutLifeArt.ByLevel(art.diplomas, lv), 106, 138);
                    break;
                case UpgradeEffect.ContactsLevel:
                    Part(g, "Telefone", art.phone, 231, 107);
                    Part(g, "Agenda", HideoutLifeArt.ByLevel(art.rolodex, lv), 456, 107);
                    break;
                case UpgradeEffect.LaundryPercent:
                    int washers = Mathf.Clamp(1 + (lv - 1) * 3 / Mathf.Max(1, g.def.maxLevel), 1, 3);
                    for (int i = 0; i < washers; i++)
                        Part(g, "Máquina de lavar dinheiro", art.washer.Length > 0 ? art.washer[0] : null, 342 + i * 44, 51);
                    break;
                case UpgradeEffect.PoliceRadioSeconds:
                    Part(g, "Rádio da polícia", RadioFrame(lv, 0), 336, 107);
                    break;
                case UpgradeEffect.SideJobPercent:
                    Part(g, "Neon do ponto de bico", art.neonOn, 474, 262);
                    break;
                case UpgradeEffect.AutoHeistSlots:
                    Part(g, "Orelhão", art.orelhao, 504, 104);
                    break;
            }
        }

        Sprite RadioFrame(int level, int frame)
        {
            if (art.radio.Length == 0) return null;
            int lv = Mathf.Clamp(level, 1, art.radio.Length / 2);
            return art.radio[Mathf.Clamp((lv - 1) * 2 + frame, 0, art.radio.Length - 1)];
        }

        bool PanelOpen => content != null && !content.ClassListContains("hidden") && content.resolvedStyle.display != DisplayStyle.None;

        void UpdateGadgets(float dt)
        {
            foreach (var g in gadgets.Values)
            {
                if (g.level <= 0) continue;
                // comprou com a aba aberta: o objeto aparece com um pulo quando a aba fecha
                if (g.revealPending && !PanelOpen)
                {
                    g.revealPending = false;
                    g.revealT = 0f;
                    var first = g.root.childCount > 0 ? g.root[0] : null;
                    var at = first != null ? Center(first) : new Vector2(320, 180);
                    Float("NOVO!", at.x, at.y + 18, new Color(0.97f, 0.83f, 0.41f), 16f, 1.6f);
                }
                if (g.revealT >= 0f)
                {
                    g.revealT += dt;
                    float k = g.revealT / 0.5f;
                    float s = k >= 1f ? 1f : Mathf.LerpUnclamped(0.2f, 1f, 1f + Mathf.Sin(k * Mathf.PI * 1.5f) * (1f - k) * 0.6f);
                    foreach (var child in g.root.Children()) child.style.scale = new Scale(new Vector3(s, s, 1f));
                    if (k >= 1f) g.revealT = -1f;
                }

                switch (g.effect)
                {
                    case UpgradeEffect.LaundryPercent:
                        int f = Mathf.FloorToInt(time * 8f) % Mathf.Max(1, art.washer.Length);
                        foreach (var child in g.root.Children()) SetSprite(child, art.washer[f]);
                        break;
                    case UpgradeEffect.PoliceRadioSeconds:
                        foreach (var child in g.root.Children()) SetSprite(child, RadioFrame(g.level, Mathf.FloorToInt(time * 3f) % 2));
                        break;
                    case UpgradeEffect.SideJobPercent:
                        // neon velho piscando de vez em quando
                        bool lit = Mathf.Repeat(time, 5.3f) > 0.35f || Mathf.Repeat(time * 13f, 2f) > 1f;
                        foreach (var child in g.root.Children()) SetSprite(child, lit ? art.neonOn : art.neonOff);
                        break;
                    case UpgradeEffect.AutoHeistSlots:
                        bool ringing = Services.AutoHeist != null && Services.AutoHeist.Running.Count > 0 && Mathf.Repeat(time, 2f) < 0.8f;
                        foreach (var child in g.root.Children()) SetSprite(child, ringing && Mathf.Repeat(time * 10f, 2f) > 1f ? art.orelhaoRinging : art.orelhao);
                        break;
                }
            }
        }

        static Vector2 Center(VisualElement e)
        {
            float x = e.style.left.value.value / 100f * W + e.style.width.value.value / 100f * W / 2f;
            float top = e.style.top.value.value / 100f * H, h = e.style.height.value.value / 100f * H;
            return new Vector2(x, H - top - h / 2f);
        }

        // ---------------- A crew no porão ----------------

        void RefreshCrew(bool force)
        {
            var crew = Services.Crew.Crew;
            var sig = new StringBuilder();
            foreach (var t in crew) sig.Append(t.id).Append(':').Append((int)t.status).Append(';');
            string s = sig.ToString();
            if (!force && s == crewSignature) return;
            crewSignature = s;

            crewLayer.Clear();
            mates.Clear();
            int slot = 0;
            foreach (var thief in crew.Where(t => t.status == ThiefStatus.Available).OrderBy(t => t.id))
            {
                if (slot >= CrewSlots.Length) break;
                var look = art.Look(thief.type);
                if (look == null || look.idle == null || look.idle.Length == 0) continue;
                var mate = new Mate { thief = thief, look = look, x = CrewSlots[slot], phase = slot * 0.37f };
                mate.body = Img(crewLayer, "ladrão: " + thief.DisplayName, look.idle[0], mate.x - 32, 2, 2f);
                if (slot % 2 == 1) mate.body.style.scale = new Scale(new Vector3(-1f, 1f, 1f)); // um de frente pro outro
                var t = thief;
                Hover(mate.body, () => $"{t.DisplayName}\nNível {t.level} · de bobeira no porão");
                mates.Add(mate);
                slot++;
            }

            photoLayer.Clear();
            photos.Clear();
            int p = 0;
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var thief in crew.Where(t => t.status == ThiefStatus.Jailed))
            {
                if (p >= PhotoSlots.Length) break;
                var spot = PhotoSlots[p++];
                var type = Services.Db.GetThiefType(thief.type);
                var photo = Img(photoLayer, "foto: " + thief.DisplayName, type != null ? type.portrait : null, spot.x + 3, spot.y + 10);
                Place(photo, spot.x + 3, spot.y + 10, 26, 27);
                photo.style.backgroundColor = new Color(0.56f, 0.62f, 0.72f);
                var stamp = Img(photoLayer, "EM CANA", art.emCanaStamp, spot.x - 3, spot.y + 4);
                stamp.style.rotate = new Rotate(new Angle(-12f));
                var t = thief;
                Hover(photo, () =>
                {
                    long left = System.Math.Max(0, t.jailReleaseUnix - System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    return $"{t.DisplayName} tá em cana\nSai em {left / 60}min {left % 60}s (ou paga a fiança na aba Crew)";
                });
                photos.Add(photo);
            }
        }

        void UpdateCrew(float dt)
        {
            foreach (var m in mates)
            {
                Sprite[] frames = m.look.idle;
                float fps = 3f;
                if (m.celebrateT >= 0f)
                {
                    m.celebrateT += dt;
                    if (m.look.celebrate != null && m.look.celebrate.Length > 0 && m.celebrateT < 1.4f)
                    {
                        frames = m.look.celebrate;
                        fps = 6f;
                    }
                    else m.celebrateT = -1f;
                }
                int i = Mathf.FloorToInt((time + m.phase) * fps) % frames.Length;
                SetSprite(m.body, frames[i]);
            }

            // de vez em quando alguém comemora e solta um bordão
            chatterIn -= dt;
            if (chatterIn <= 0f && mates.Count > 0)
            {
                chatterIn = Random.Range(7f, 13f);
                var m = mates[Random.Range(0, mates.Count)];
                m.celebrateT = 0f;
                var barks = Services.Db.GetThiefType(m.thief.type)?.barks;
                if (barks != null && barks.Length > 0) Say(barks[Random.Range(0, barks.Length)], m.x, 70);
            }
            if (bubbleLeft > 0f)
            {
                bubbleLeft -= dt;
                if (bubbleLeft <= 0f) bubble.style.display = DisplayStyle.None;
            }
        }

        // ---------------- Textos: "+Cr$", balão e dica do mouse ----------------

        void BuildTip()
        {
            tip = new Label { name = "dica", pickingMode = PickingMode.Ignore };
            Style(tip, new Color(0.925f, 0.824f, 0.627f));
            tip.style.display = DisplayStyle.None;
            tip.style.maxWidth = 190;
            tip.style.whiteSpace = WhiteSpace.Normal;
            layer.Add(tip);

            bubble = new Label { name = "balão", pickingMode = PickingMode.Ignore };
            Style(bubble, Cream);
            bubble.style.display = DisplayStyle.None;
            bubble.style.maxWidth = 150;
            bubble.style.whiteSpace = WhiteSpace.Normal;
            layer.Add(bubble);
        }

        static void Style(VisualElement e, Color bg)
        {
            e.style.position = Position.Absolute;
            e.style.backgroundColor = bg;
            e.style.color = Ink;
            e.style.paddingLeft = e.style.paddingRight = 4;
            e.style.paddingTop = e.style.paddingBottom = 2;
            e.style.borderLeftWidth = e.style.borderRightWidth = e.style.borderTopWidth = e.style.borderBottomWidth = 1;
            e.style.borderLeftColor = e.style.borderRightColor = e.style.borderTopColor = e.style.borderBottomColor = Ink;
        }

        void ShowTip(string text, Vector2 panelPos)
        {
            tip.text = text;
            tip.style.display = DisplayStyle.Flex;
            tip.BringToFront();
            MoveTip(panelPos);
        }

        void MoveTip(Vector2 panelPos)
        {
            var local = layer.WorldToLocal(panelPos);
            float w = layer.resolvedStyle.width;
            tip.style.left = Mathf.Min(local.x + 10, Mathf.Max(0, w - 200));
            tip.style.top = Mathf.Max(30, local.y - 34);
        }

        void Say(string line, float x, float y)
        {
            bubble.text = line;
            bubble.style.display = DisplayStyle.Flex;
            bubble.style.left = Length.Percent(Mathf.Clamp(x - 40, 100, 480) / W * 100f);
            bubble.style.top = StyleKeyword.Auto;
            bubble.style.bottom = Length.Percent(y / H * 100f);
            bubbleLeft = 3f;
        }

        void Float(string text, float x, float y, Color color, float rise, float life)
        {
            // etiqueta escura com o texto e sombra de 1 px: lê em cima de qualquer coisa (tela da TV, tijolo, Kombi)
            var tag = new VisualElement { pickingMode = PickingMode.Ignore };
            tag.style.position = Position.Absolute;
            tag.style.left = Length.Percent(x / W * 100f);
            tag.style.bottom = Length.Percent(y / H * 100f);
            tag.style.translate = new Translate(Length.Percent(-50), 0);
            tag.style.backgroundColor = new Color(Ink.r, Ink.g, Ink.b, 0.8f);
            tag.style.paddingLeft = tag.style.paddingRight = 3;
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.style.color = color;
            label.style.marginLeft = label.style.marginRight = label.style.marginTop = label.style.marginBottom = 0;
            label.style.paddingLeft = label.style.paddingRight = label.style.paddingTop = label.style.paddingBottom = 0;
            label.style.textShadow = new TextShadow { offset = new Vector2(1f, 1f), color = Ink, blurRadius = 0f };
            tag.Add(label);
            layer.Add(tag);
            floaters.Add(new Floater { el = tag, life = life, rise = rise });
        }

        void UpdateFloaters(float dt)
        {
            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                var f = floaters[i];
                f.t += dt;
                float k = f.t / f.life;
                f.el.style.translate = new Translate(Length.Percent(-50), -f.rise * Mathf.Sin(Mathf.Min(1f, k * 1.6f) * Mathf.PI / 2f));
                f.el.style.opacity = k < 0.65f ? 1f : Mathf.Clamp01(1f - (k - 0.65f) / 0.35f);
                if (k >= 1f)
                {
                    f.el.RemoveFromHierarchy();
                    floaters.RemoveAt(i);
                }
            }
        }

        /// <summary>Quantos "+Cr$" estão subindo agora (pros testes).</summary>
        public int FloatingCount => floaters.Count;
        public IEnumerable<VisualElement> Floating => floaters.Select(f => f.el);

        // ---------------- Todo frame ----------------

        void Update()
        {
            if (!TryBuild()) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (GameClock.Paused) dt = 0f;
            time += dt;
            pollIn -= dt;
            bool poll = pollIn <= 0f;
            if (poll)
            {
                pollIn = 0.5f;
                RefreshCrew(force: false);
            }
            UpdateMonitors(dt, poll);
            UpdateGadgets(dt);
            UpdateCrew(dt);
            UpdateFloaters(dt);
        }
    }
}
