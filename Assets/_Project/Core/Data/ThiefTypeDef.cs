using UnityEngine;

namespace AFirma.Core
{
    [CreateAssetMenu(menuName = "A Firma/Tipo de Ladrão", fileName = "ThiefType")]
    public class ThiefTypeDef : DefBase
    {
        public ThiefType type;

        [Header("Especialidade")]
        public bool hasSpecialty = true;
        public ObstacleType specialty;

        [Header("Atributos no nível 1 e ganho por nível")]
        public float baseSkill = 30f;
        public float baseStealth = 30f;
        public float baseNerves = 30f;
        public float skillPerLevel = 7f;
        public float stealthPerLevel = 4f;
        public float nervesPerLevel = 4f;

        [Header("Passiva de fuga (usada pelo Pé-na-Tábua)")]
        [Tooltip("Segundos a mais na contagem da polícia.")]
        public float policeCountdownBonus;
        [Tooltip("Fração extra da sacola mantida em fuga apressada/forçada (0.1 = +10%).")]
        public float escapeLootBonus;
        [Tooltip("Redução na chance de cada ladrão ser preso na fuga forçada (0.1 = -10 pontos).")]
        public float arrestChanceReduction;

        [Header("Habilidade e bico")]
        public ThiefAbilityDef ability;
        public SideJobDef signatureSideJob;

        [Header("Visual e áudio (dono do ladrão preenche)")]
        public Sprite portrait;
        public RuntimeAnimatorController animator;
        public Color color = Color.white;
        public AudioClip actionSfx;
        [TextArea(1, 2)] public string[] barks;
    }
}
