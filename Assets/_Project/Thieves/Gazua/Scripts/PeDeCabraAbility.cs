using AFirma.Core;
using UnityEngine;

namespace AFirma.Thieves.Gazua
{
    /// <summary>
    /// Pé-de-cabra, a ativa do Gazua: abre na marra a sala de Fechadura em que a crew está (limpa na hora),
    /// mas faz barulho (+alerta). O botão diz o motivo quando não dá: fora de sala de Fechadura ou na porta.
    /// O som fica em sfx e o efeito visual em vfxPrefab (a Cena do Golpe instancia na sala).
    /// </summary>
    [CreateAssetMenu(menuName = "A Firma/Habilidades/Pé-de-cabra", fileName = "Ability_Gazua")]
    public class PeDeCabraAbility : ThiefAbilityDef
    {
        [Tooltip("Alerta que o barulho do pé-de-cabra gera.")]
        public float noise = 15f;

        public const string OnlyLockRoom = "Só serve em sala de Fechadura.";
        public const string WaitForTheRoom = "Espera a crew entrar na sala.";

        public override bool CanUse(IHeistControl heist, out string reason)
        {
            if (heist.Phase == HeistPhase.AwaitingDoor)
            {
                reason = WaitForTheRoom;
                return false;
            }
            if (!base.CanUse(heist, out reason)) return false;
            if (heist.CurrentRoom == null || heist.CurrentRoom.obstacle != ObstacleType.Lock)
            {
                reason = OnlyLockRoom;
                return false;
            }
            reason = null;
            return true;
        }

        public override void Use(IHeistControl heist)
        {
            heist.AddAlert(noise);
            heist.CompleteCurrentRoom();
        }
    }
}
