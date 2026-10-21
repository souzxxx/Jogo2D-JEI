using System.Collections.Generic;
using UnityEngine;

namespace AFirma.Core
{
    /// <summary>Números globais do jogo. Nada de número mágico no código: vem daqui ou de um Def.</summary>
    [CreateAssetMenu(menuName = "A Firma/Config Global", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Começo de jogo")]
        public double startingMoney = 0;
        public List<ThiefType> startingCrew = new List<ThiefType> { ThiefType.ConArtist, ThiefType.Locksmith };
        public int startingCrewSlots = 3;
        public int startingHeistSlots = 2;

        [Header("Golpe")]
        [Range(0f, 1f)] public float eventChancePerRoom = 0.3f;
        [Tooltip("Fração da maior Perícia usada quando falta o especialista.")]
        public float noSpecialistPowerFactor = 0.35f;
        public float noSpecialistAlertMult = 2f;
        [Tooltip("Quanto a Discrição média reduz o alerta (0.5 = até -50%).")]
        public float stealthAlertFactor = 0.5f;
        public float policeCountdownSeconds = 10f;
        [Tooltip("A contagem nunca fica menor que isso, nem com Blitz na área.")]
        public float minPoliceCountdownSeconds = 3f;
        [Range(0f, 1f)] public float hastyEscapeLootKeep = 0.8f;
        [Range(0f, 1f)] public float forcedEscapeLootKeep = 0.5f;
        [Range(0f, 1f)] public float baseArrestChance = 0.4f;
        [Tooltip("Quanto a Discrição do ladrão reduz a chance de ele ser preso (0.5 = até -50%).")]
        public float stealthArrestFactor = 0.5f;
        public int xpPerRoom = 1;
        public float speedFast = 2f;

        [Header("Crew")]
        public int maxLevel = 10;
        [Tooltip("XP pro próximo nível = fator × nível atual.")]
        public int xpPerLevelFactor = 5;
        public int recruitCandidates = 3;
        public float recruitRefreshSeconds = 300f;
        public double recruitBaseCost = 50;
        public float recruitCostGrowthPerLevel = 1.8f;
        [Tooltip("Trocar os candidatos na hora, pagando.")]
        public double recruitRefreshCost = 25;
        [Tooltip("Desconto na troca paga por nível de Contatos (0.1 = -10% por nível).")]
        public float recruitRefreshDiscountPerContacts = 0.1f;
        public double bailCostPerJailSecond = 2;

        [Header("Dona Cida (anti dead-end)")]
        public string mentorName = "Dona Cida";
        [TextArea(1, 3), Tooltip("Bip quando a crew está toda presa e sem Cr$ pra recrutar. {nome} vira o nome do novato.")]
        public string freeRookieBip = "Tá todo mundo em cana e o caixa tá zerado? Mandei o {nome} pra dar uma força. Não me faz passar vergonha.";

        [Header("Economia")]
        public double scavengeIncomePerSecond = 1; // Catar latinha
        public float offlineCapHours = 4f;
        [Range(0f, 1f)] public float offlineRate = 0.5f;

        [Header("Prestígio")]
        public double fameDivisor = 10000;
        public float wantedDifficultyMult = 1.25f;
        public float wantedLootMult = 1.5f;
        public float wantedAlertMult = 1.1f;

        [Header("Save")]
        public float autosaveSeconds = 10f;
    }
}
