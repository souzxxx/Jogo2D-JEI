using UnityEngine;
using UnityEngine.SceneManagement;

namespace AFirma.Core
{
    /// <summary>
    /// Diretor da abertura (ESCOPO 5 e 10): Novo jogo → Abertura → Esconderijo, e o "Ver abertura" do menu.
    ///
    /// Põe o IntroHeistService no lugar do motor e abre a cena Heist; a cena e a HUD tocam a abertura como um golpe
    /// qualquer. No fim (40 s), no Pular (botão ou Esc) ou se alguém sair da cena, devolve o motor de verdade.
    /// Não mexe no save: só grava introSeen. No Novo jogo, o bip da segunda chance substitui o de jogo novo
    /// (a Narrativa não dá bip de marco enquanto IsPlaying). Versão mínima (sem Intro.asset): só o bip.
    ///
    /// Telas (Enzo): escute IntroStarted, IntroCaption (letreiro; null = some) e IntroFinished, chame Skip() no Pular
    /// e desligue ShowPlaceholderOverlay quando a tela de verdade existir.
    /// </summary>
    public static class IntroDirector
    {
        /// <summary>Letreiro e botão Pular provisórios (OnGUI) até a tela da UI existir.</summary>
        public static bool ShowPlaceholderOverlay = true;

        public static bool IsPlaying { get; private set; }
        /// <summary>"Ver abertura" do menu: não mexe em nada e volta pro menu.</summary>
        public static bool IsReplay { get; private set; }
        public static IntroHeistService Current { get; private set; }

        static IHeistService realHeist;
        static bool bipSent;
        static GameObject runner;

        /// <summary>Novo jogo do menu: começa o save do zero e toca a abertura, que termina no esconderijo.</summary>
        public static void NewGameWithIntro()
        {
            IsPlaying = true; // antes do StartNewGame, pra a Narrativa não mandar o bip de jogo novo
            IsReplay = false;
            Services.StartNewGame();
            Begin();
        }

        /// <summary>"Ver abertura": toca de novo sem mexer no save e volta pro menu.</summary>
        public static void Replay()
        {
            IsPlaying = true;
            IsReplay = true;
            Begin();
        }

        /// <summary>Pular (botão ou Esc): vai direto pro fim, com o bip da segunda chance se for jogo novo.</summary>
        public static void Skip() => End(loadNextScene: true);

        static void Begin()
        {
            var def = Services.Db != null ? Services.Db.intro : null;
            Current = def != null ? new IntroHeistService(def, Services.Db) : null;
            if (Current == null || !Current.CanStart(def.heist, Current.Crew, out _))
            {
                Current = null;
                End(loadNextScene: true); // versão mínima: só o bip
                return;
            }

            realHeist = Services.Heist;
            Services.Register((IHeistService)Current);
            bipSent = false;
            runner = new GameObject("[Abertura]");
            Object.DontDestroyOnLoad(runner);
            runner.AddComponent<IntroRunner>();
            GameEvents.RaiseIntroStarted(IsReplay);
            SceneFlow.GoToHeist(def.heist, Current.Crew);
        }

        /// <summary>Chamado todo frame pelo IntroRunner: bip no BipAt e fim no Length.</summary>
        internal static void Update()
        {
            if (!IsPlaying || Current == null) return;
            if (Current.Time >= IntroHeistService.BipAt) SendBip();
            if (Current.Time >= IntroHeistService.Length) End(loadNextScene: true);
        }

        /// <summary>Saiu da cena Heist por outro caminho (ex.: "Voltar pro esconderijo" no jornal): encerra sem trocar de cena.</summary>
        internal static void OnSceneChanged(Scene next)
        {
            if (IsPlaying && Current != null && next.name != SceneFlow.Heist) End(loadNextScene: false);
        }

        static void SendBip()
        {
            if (bipSent || IsReplay) return;
            bipSent = true;
            var text = Services.Db != null && Services.Db.intro != null ? Services.Db.intro.secondChanceBip : null;
            if (!string.IsNullOrEmpty(text))
                GameEvents.RaiseBip(Services.Config != null ? Services.Config.mentorName : "Dona Cida", text);
        }

        static void End(bool loadNextScene)
        {
            if (!IsPlaying) return;
            SendBip();
            IsPlaying = false;
            if (realHeist != null) Services.Register(realHeist);
            realHeist = null;
            Current = null;
            if (runner != null) Object.Destroy(runner);
            runner = null;
            if (!IsReplay && Services.State != null) Services.State.introSeen = true;
            GameEvents.RaiseIntroCaption(null);
            GameEvents.RaiseIntroFinished();

            if (!loadNextScene) Services.SaveNow();
            else if (IsReplay) SceneFlow.GoToMainMenu();
            else SceneFlow.GoToHideout();
        }

        /// <summary>Zera tudo (início do Play e testes).</summary>
        public static void Reset()
        {
            IsPlaying = false;
            IsReplay = false;
            Current = null;
            realHeist = null;
            bipSent = false;
            if (runner != null) Object.Destroy(runner);
            runner = null;
            ShowPlaceholderOverlay = true;
        }
    }
}
