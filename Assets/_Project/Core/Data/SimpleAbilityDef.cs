using UnityEngine;

namespace AFirma.Core
{
    /// <summary>
    /// Habilidade configurável só com dados. Serve de placeholder até o dono do ladrão
    /// criar a versão dele (com efeitos visuais/sonoros próprios).
    /// </summary>
    [CreateAssetMenu(menuName = "A Firma/Habilidade simples", fileName = "Ability")]
    public class SimpleAbilityDef : ThiefAbilityDef
    {
        public float alertDelta;
        public float freezeAlertSeconds;
        public bool completeCurrentRoom;
        public bool requireRoomObstacle;
        public ObstacleType roomObstacle;
        public bool perfectEscape;

        public override bool CanUse(IHeistControl heist, out string reason)
        {
            if (perfectEscape && heist.Phase == HeistPhase.AwaitingEvent)
            {
                reason = "Resolve o evento primeiro.";
                return false;
            }
            if (!perfectEscape && !base.CanUse(heist, out reason)) return false;
            if (requireRoomObstacle && (heist.CurrentRoom == null || heist.CurrentRoom.obstacle != roomObstacle || heist.Phase != HeistPhase.Working))
            {
                reason = "Não tem esse tipo de obstáculo nesta sala.";
                return false;
            }
            if (alertDelta < 0f && heist.Alert <= 0f)
            {
                reason = "Ninguém desconfia de nada ainda."; // baixar alerta zerado só gastaria a habilidade
                return false;
            }
            reason = null;
            return true;
        }

        /// <summary>Fuga perfeita: com a polícia chegando. Sala de um obstáculo: nela. Baixar ou congelar o alerta: com alerta alto.</summary>
        public override AbilityAutoUse AutoUse =>
            perfectEscape ? AbilityAutoUse.PoliceArriving
            : completeCurrentRoom && requireRoomObstacle ? AbilityAutoUse.MatchingRoom
            : alertDelta < 0f || freezeAlertSeconds > 0f ? AbilityAutoUse.HighAlert
            : AbilityAutoUse.Never;

        public override void Use(IHeistControl heist)
        {
            if (alertDelta != 0f) heist.AddAlert(alertDelta);
            if (freezeAlertSeconds > 0f) heist.FreezeAlert(freezeAlertSeconds);
            if (completeCurrentRoom) heist.CompleteCurrentRoom();
            if (perfectEscape) heist.PerfectEscape();
        }
    }
}
