using System.Collections.Generic;

namespace AFirma.Core
{
    /// <summary>
    /// Golpes no automático (ESCOPO 6.11): a crew repete sozinha um golpe que você já limpou na mão, sem cena,
    /// rodando junto com o golpe na mão. Dono: Leonardo. Mock: MockAutoHeistService.
    ///
    /// Não dispara os eventos do golpe na mão (a cena e a HUD não reagem): só os AutoHeist* do GameEvents.
    /// </summary>
    public interface IAutoHeistService
    {
        /// <summary>Os que estão rodando agora, na ordem em que foram mandados.</summary>
        IReadOnlyList<AutoHeistRun> Running { get; }
        /// <summary>Quantos ao mesmo tempo: startingAutoHeistSlots + Orelhão.</summary>
        int MaxRunning { get; }

        /// <summary>Já pode ir pro automático? Só depois de limpar o cofre na mão neste ciclo.</summary>
        bool IsAutoUnlocked(HeistDef heist);
        bool CanSend(HeistDef heist, IReadOnlyList<ThiefState> crew, out string reason);
        /// <summary>Manda a crew. Ela fica "Em golpe" (sai do bico) até a volta acabar sem Repetir.</summary>
        void Send(HeistDef heist, IReadOnlyList<ThiefState> crew, AutoHeistOrder order, bool repeat);

        void SetRepeat(AutoHeistRun run, bool repeat);
        /// <summary>Chamar de volta: a crew mete o pé agora (antes da sirene sai limpo, na contagem sai apressado).</summary>
        void Recall(AutoHeistRun run);
    }
}
