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
            reason = null;
            return true;
        }

        public override void Use(IHeistControl heist)
        {
            if (alertDelta != 0f) heist.AddAlert(alertDelta);
            if (freezeAlertSeconds > 0f) heist.FreezeAlert(freezeAlertSeconds);
            if (completeCurrentRoom) heist.CompleteCurrentRoom();
            if (perfectEscape) heist.PerfectEscape();
        }
    }
}
