using UnityEngine;

namespace AFirma.Core
{
    /// <summary>
    /// Habilidade ativa de um tipo de ladrão (uso único por golpe).
    /// Cada dono de ladrão cria uma subclasse na pasta do seu ladrão e mexe no golpe só pelo IHeistControl.
    /// </summary>
    public abstract class ThiefAbilityDef : DefBase
    {
        public AudioClip sfx;
        [Tooltip("Efeito visual da habilidade. A Cena do Golpe instancia na sala atual quando escuta AbilityUsed.")]
        public GameObject vfxPrefab;

        public virtual bool CanUse(IHeistControl heist, out string reason)
        {
            if (heist.Phase != HeistPhase.Working && heist.Phase != HeistPhase.AwaitingDoor)
            {
                reason = "Só dá pra usar com o golpe rolando.";
                return false;
            }
            reason = null;
            return true;
        }

        public abstract void Use(IHeistControl heist);

        /// <summary>Quando o piloto dos golpes no automático usa esta habilidade sozinho. Never = só na mão.</summary>
        public virtual AbilityAutoUse AutoUse => AbilityAutoUse.Never;
    }
}
