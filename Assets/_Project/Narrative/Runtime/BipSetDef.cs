using System;
using System.Collections.Generic;
using AFirma.Core;
using UnityEngine;

namespace AFirma.Narrative
{
    /// <summary>Textos dos bips da Dona Cida nos marcos (ESCOPO 5). Tom: Narrative/LEIA-ME.md.</summary>
    [CreateAssetMenu(menuName = "A Firma/Bips da Dona Cida", fileName = "BipSet")]
    public class BipSetDef : ScriptableObject
    {
        public const string ResourcePath = "DonaCidaBips";

        [TextArea(2, 4)] public string newGame;
        [TextArea(2, 4), Tooltip("Primeiro golpe com o cofre limpo.")] public string firstHeistCompleted;
        [TextArea(2, 4), Tooltip("Primeiro golpe em que a crew meteu o pé antes do cofre.")] public string firstHeistFled;
        [TextArea(2, 4)] public string firstSiren;
        [TextArea(2, 4), Tooltip("{nome} vira o nome do ladrão preso.")] public string firstArrest;
        [Tooltip("Um bip por golpe liberado. O do último golpe da escada é o do Banco Central.")]
        public List<HeistBip> heistUnlocked = new List<HeistBip>();
        [TextArea(2, 4), Tooltip("Último golpe da escada concluído: o prestígio fica disponível.")] public string prestigeUnlocked;

        public string GetHeistUnlocked(HeistDef heist)
        {
            if (heist == null) return null;
            var entry = heistUnlocked.Find(b => b.heist != null && b.heist.id == heist.id);
            return entry != null ? entry.text : null;
        }
    }

    [Serializable]
    public class HeistBip
    {
        public HeistDef heist;
        [TextArea(2, 4)] public string text;
    }
}
