using System.Collections.Generic;
using UnityEngine;

namespace AFirma.Core
{
    /// <summary>
    /// Ponto único de acesso a todos os dados do jogo. Fica em Data/Resources/GameDatabase.asset.
    /// Criou um Def novo? Coloca ele na lista certa aqui, senão o jogo não enxerga.
    /// </summary>
    [CreateAssetMenu(menuName = "A Firma/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public const string ResourcePath = "GameDatabase";

        public GameConfig config;
        public NamePoolDef names;
        public HeadlineSetDef headlines;

        public List<ThiefTypeDef> thiefTypes = new List<ThiefTypeDef>();
        public List<TraitDef> traits = new List<TraitDef>();
        public List<HeistDef> heists = new List<HeistDef>();
        public List<HeistEventDef> events = new List<HeistEventDef>();
        public List<UpgradeDef> upgrades = new List<UpgradeDef>();
        public List<SideJobDef> sideJobs = new List<SideJobDef>();
        public List<HeistModifierDef> modifiers = new List<HeistModifierDef>();
        public List<FamePerkDef> famePerks = new List<FamePerkDef>();

        public ThiefTypeDef GetThiefType(ThiefType type) => thiefTypes.Find(t => t != null && t.type == type);
        public TraitDef GetTrait(string id) => Find(traits, id);
        public HeistDef GetHeist(string id) => Find(heists, id);
        public HeistEventDef GetEvent(string id) => Find(events, id);
        public UpgradeDef GetUpgrade(string id) => Find(upgrades, id);
        public UpgradeDef GetUpgrade(UpgradeEffect effect) => upgrades.Find(u => u != null && u.effect == effect);
        public SideJobDef GetSideJob(string id) => Find(sideJobs, id);
        public HeistModifierDef GetModifier(string id) => Find(modifiers, id);
        public FamePerkDef GetFamePerk(string id) => Find(famePerks, id);

        static T Find<T>(List<T> list, string id) where T : DefBase
        {
            if (string.IsNullOrEmpty(id)) return null;
            return list.Find(d => d != null && d.id == id);
        }
    }
}
