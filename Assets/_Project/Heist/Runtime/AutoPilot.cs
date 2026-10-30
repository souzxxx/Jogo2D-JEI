using System;
using AFirma.Core;

namespace AFirma.Heist
{
    /// <summary>
    /// Piloto dos golpes no automático (ESCOPO 6.11). Joga um HeistRunner sem cena depois de cada Tick:
    /// portas pela ordem, no evento a escolha com melhor chance que a crew pode fazer e as habilidades na hora certa
    /// (Lábia e Loop com alerta alto, Pé-de-cabra em Fechadura, Cavalo de pau com a polícia chegando).
    /// </summary>
    public static class AutoPilot
    {
        /// <summary>Resolve o que está esperando decisão, usa habilidade e mete o pé se a ordem mandar.</summary>
        public static void Step(HeistRunner runner, AutoHeistOrder order, GameConfig cfg)
        {
            // Cavalo de pau antes da porta: com sirene, ele é melhor que a fuga apressada.
            UseAbilities(runner, order, cfg);

            // Porta pode abrir evento; o limite só protege de loop infinito.
            for (int i = 0; i < 16 && runner.IsRunning; i++)
            {
                if (runner.Phase == HeistPhase.AwaitingEvent) runner.ChooseEventOption(PickChoice(runner.PendingEvent));
                else if (runner.Phase == HeistPhase.AwaitingDoor) runner.ChooseDoor(GoToNextRoom(runner, order, cfg));
                else break;
            }
            if (!runner.IsRunning) return;

            UseAbilities(runner, order, cfg);
            if (runner.IsRunning && runner.Phase == HeistPhase.Working && ShouldFlee(runner, order, cfg)) runner.Flee();
        }

        /// <summary>A escolha disponível com maior chance de sucesso; no empate, a primeira.</summary>
        public static int PickChoice(HeistEventInstance instance)
        {
            int best = -1;
            for (int i = 0; i < instance.choices.Count; i++)
            {
                var choice = instance.choices[i];
                if (choice.available && (best < 0 || choice.successChance > instance.choices[best].successChance)) best = i;
            }
            return Math.Max(0, best);
        }

        /// <summary>
        /// Até o cofre: sempre segue, a não ser com sirene e sem Cavalo de pau.
        /// Na moita: para com sirene, com alerta no limite ou se a próxima sala sozinha já tocaria a sirene.
        /// </summary>
        public static bool GoToNextRoom(HeistRunner runner, AutoHeistOrder order, GameConfig cfg)
        {
            if (runner.SirenActive) return order == AutoHeistOrder.ToTheVault && HasEscapeLeft(runner);
            if (order == AutoHeistOrder.ToTheVault) return true;
            if (runner.Alert >= cfg.autoQuietAlertLimit) return false;
            var door = runner.PendingDoor;
            return door == null || runner.Alert + AlertForRoom(runner, door.nextRoom, door.nextRoomIndex) < 100f;
        }

        /// <summary>Com a crew trabalhando: na sirene mete o pé (a não ser que o Cavalo de pau ainda vá salvar); na moita, também no limite de alerta.</summary>
        public static bool ShouldFlee(HeistRunner runner, AutoHeistOrder order, GameConfig cfg)
        {
            if (runner.SirenActive) return !HasEscapeLeft(runner);
            return order == AutoHeistOrder.Quiet && runner.Alert >= cfg.autoQuietAlertLimit;
        }

        /// <summary>Alerta que a crew juntaria limpando a sala inteira (sem habilidade e sem evento).</summary>
        public static float AlertForRoom(HeistRunner runner, RoomData room, int roomIndex)
        {
            var crew = runner.CurrentCrew;
            float power = HeistRules.RoomPower(room.obstacle, crew, Services.Crew, Services.Db);
            float seconds = HeistRules.RoomSeconds(room, runner.Context, power);
            float fame = Services.Progression.GetFamePerkTotal(FamePerkEffect.AlertReductionPercent);
            return HeistRules.AlertPerSecond(room, roomIndex, runner.Context, crew, Services.Crew, Services.Db, fame) * seconds;
        }

        /// <summary>Uma habilidade por passo, a do primeiro ladrão que está na hora.</summary>
        static void UseAbilities(HeistRunner runner, AutoHeistOrder order, GameConfig cfg)
        {
            foreach (var thief in runner.CurrentCrew)
            {
                if (!runner.IsRunning) return;
                var ability = AbilityOf(thief);
                if (ability == null || !runner.CanUseAbility(thief, out _) || !IsTime(ability.AutoUse, runner, order, cfg)) continue;
                runner.UseAbility(thief);
                return;
            }
        }

        static bool IsTime(AbilityAutoUse when, HeistRunner runner, AutoHeistOrder order, GameConfig cfg)
        {
            switch (when)
            {
                // Depois da sirene baixar o alerta não segura a polícia: guarda pra não gastar à toa.
                case AbilityAutoUse.HighAlert:
                    return !runner.SirenActive && runner.Phase == HeistPhase.Working && runner.Alert >= cfg.autoAbilityAlert;
                case AbilityAutoUse.MatchingRoom:
                    return !runner.SirenActive && runner.Phase == HeistPhase.Working && runner.Alert < cfg.autoAbilityAlert;
                // Na moita sai na hora; até o cofre, a crew ainda trabalha até a polícia estar na porta.
                case AbilityAutoUse.PoliceArriving:
                    return runner.SirenActive && (order == AutoHeistOrder.Quiet || runner.PoliceSecondsLeft <= cfg.autoEscapeSecondsLeft);
                default:
                    return false;
            }
        }

        static bool HasEscapeLeft(HeistRunner runner)
        {
            foreach (var thief in runner.CurrentCrew)
            {
                var ability = AbilityOf(thief);
                if (ability != null && ability.AutoUse == AbilityAutoUse.PoliceArriving && !runner.HasUsedAbility(thief)) return true;
            }
            return false;
        }

        static ThiefAbilityDef AbilityOf(ThiefState thief) => Services.Db.GetThiefType(thief.type)?.ability;
    }
}
