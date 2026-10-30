using System.Collections.Generic;
using AFirma.Core;

namespace AFirma.Narrative
{
    /// <summary>
    /// Escuta os GameEvents e manda o bip da Dona Cida nos marcos, uma vez por save.
    ///
    /// Os bips entram numa fila e só saem no Flush (chamado todo frame fora da pausa), pra a tela já estar
    /// ouvindo quando o jogo carrega. Golpe liberado no meio do golpe espera o golpe acabar, pra o bip do
    /// resultado vir antes do "tem golpe novo no mapa".
    /// O que já foi dito fica em GameState.seenTips com o prefixo "bip:". O novato grátis é do CrewService.
    /// </summary>
    public class DonaCidaBips
    {
        public const string SeenPrefix = "bip:";

        readonly BipSetDef texts;
        readonly List<string> queue = new List<string>();
        readonly List<string> afterHeist = new List<string>();
        GameState currentState;

        static GameState S => Services.State;

        public DonaCidaBips(BipSetDef texts)
        {
            this.texts = texts;
            currentState = S;
            GameEvents.GameLoaded += OnGameStarted;
            GameEvents.NewGameStarted += OnGameStarted;
            GameEvents.SirenStarted += OnSirenStarted;
            GameEvents.ThiefJailed += OnThiefJailed;
            GameEvents.HeistUnlocked += OnHeistUnlocked;
            GameEvents.HeistFinished += OnHeistFinished;
            GameEvents.Prestiged += OnPrestiged;
        }

        public void Dispose()
        {
            GameEvents.GameLoaded -= OnGameStarted;
            GameEvents.NewGameStarted -= OnGameStarted;
            GameEvents.SirenStarted -= OnSirenStarted;
            GameEvents.ThiefJailed -= OnThiefJailed;
            GameEvents.HeistUnlocked -= OnHeistUnlocked;
            GameEvents.HeistFinished -= OnHeistFinished;
            GameEvents.Prestiged -= OnPrestiged;
        }

        public int Pending => queue.Count;

        /// <summary>Manda os bips da fila. Na pausa eles esperam.</summary>
        public void Flush()
        {
            if (GameClock.Paused || queue.Count == 0) return;
            var sender = Services.Config != null ? Services.Config.mentorName : "Dona Cida";
            var pending = queue.ToArray();
            queue.Clear();
            foreach (var message in pending) GameEvents.RaiseBip(sender, message);
        }

        // ---------------- Marcos ----------------

        /// <summary>Jogo carregado ou novo: bip de boas-vindas se a firma ainda não fez nenhum golpe.</summary>
        public void OnGameStarted()
        {
            if (S != currentState)
            {
                queue.Clear(); afterHeist.Clear(); // save trocado: o que estava na fila era do outro jogo
                currentState = S;
            }
            if (S != null && S.completedHeists.Count == 0) Say("jogo-novo", texts.newGame, queue);
        }

        void OnSirenStarted(float seconds) => Say("primeira-sirene", texts.firstSiren, queue);

        void OnThiefJailed(ThiefState thief)
        {
            if (thief == null || string.IsNullOrEmpty(texts.firstArrest)) return;
            Say("primeiro-preso", texts.firstArrest.Replace("{nome}", thief.DisplayName), queue);
        }

        void OnHeistUnlocked(HeistDef heist)
        {
            if (heist == null) return;
            bool heistRunning = Services.Heist != null && Services.Heist.IsRunning;
            Say("liberado:" + heist.id, texts.GetHeistUnlocked(heist), heistRunning ? afterHeist : queue);
        }

        void OnHeistFinished(HeistResult result)
        {
            if (result != null)
            {
                Say("primeiro-golpe", result.completed ? texts.firstHeistCompleted : texts.firstHeistFled, queue);
                if (result.completed && IsLastHeist(result.heistId)) Say("prestigio", texts.prestigeUnlocked, queue);
            }
            queue.AddRange(afterHeist);
            afterHeist.Clear();
        }

        /// <summary>Topo da escada: golpe extra nunca é o último, mesmo com tier alto.</summary>
        static bool IsLastHeist(string heistId)
        {
            var heist = Services.Db.GetHeist(heistId);
            if (heist == null || heist.extra) return false;
            foreach (var other in Services.Db.heists)
                if (other != null && !other.extra && other.tier > heist.tier) return false;
            return true;
        }

        /// <summary>Todo prestígio tem bip (não só o primeiro): a firma recomeça do zero e a Dona Cida manda os novatos.</summary>
        void OnPrestiged(int fameGained, int wantedLevel)
        {
            if (string.IsNullOrEmpty(texts.prestiged)) return;
            queue.Add(texts.prestiged.Replace("{fama}", fameGained.ToString()).Replace("{procurado}", wantedLevel.ToString()));
        }

        /// <summary>Põe na fila se ainda não foi dito neste save. Texto vazio não vira bip.</summary>
        static void Say(string id, string text, List<string> target)
        {
            if (S == null || string.IsNullOrEmpty(text)) return;
            var key = SeenPrefix + id;
            if (S.seenTips.Contains(key)) return;
            S.seenTips.Add(key);
            target.Add(text);
        }
    }
}
