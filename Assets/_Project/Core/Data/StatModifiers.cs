using System;

namespace AFirma.Core
{
    /// <summary>Multiplicadores usados por manhas (traços). 1 = sem efeito.</summary>
    [Serializable]
    public class StatModifiers
    {
        public float skillMult = 1f;
        public float stealthMult = 1f;
        public float nervesMult = 1f;
        public float lootMult = 1f;
        public float personRoomLootMult = 1f;
        public float alertMult = 1f;
        public float xpMult = 1f;
        public float bailMult = 1f;
        public float badEventChanceMult = 1f;
    }
}
