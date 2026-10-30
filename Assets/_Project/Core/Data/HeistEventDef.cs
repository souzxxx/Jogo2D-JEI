using System;
using System.Collections.Generic;
using UnityEngine;

namespace AFirma.Core
{
    [Serializable]
    public class EventEffect
    {
        [TextArea(1, 3)] public string resultText;
        public float alertDelta;
        [Tooltip("Variação percentual da sacola. -20 = perde 20% do que já pegou.")]
        public float bagPercentDelta;
        [Tooltip("Segundos perdidos (trava o progresso da sala).")]
        public float delaySeconds;
        public bool jailRandomThief;
    }

    [Serializable]
    public class EventChoice
    {
        public string label;

        [Header("Requisito (opcional)")]
        public bool requiresType;
        public ThiefType requiredType;

        [Header("Chance de sucesso")]
        public CheckStat check = CheckStat.None;
        [Range(0f, 1f)] public float baseChance = 1f;
        [Tooltip("Chance somada por ponto do atributo checado (do melhor ladrão da crew).")]
        public float chancePerStatPoint;

        public EventEffect success = new EventEffect();
        public EventEffect failure = new EventEffect();
    }

    [CreateAssetMenu(menuName = "A Firma/Evento de Golpe", fileName = "HeistEvent")]
    public class HeistEventDef : DefBase
    {
        [TextArea(2, 6)] public string body;
        public Sprite art;

        [Header("Onde pode aparecer")]
        [Tooltip("Só aparece pelo tutorial (não entra no sorteio).")]
        public bool tutorialOnly;
        public bool anyRoom = true;
        public ObstacleType roomObstacle;
        [Tooltip("Golpe mínimo (tier) em que pode aparecer.")]
        public int minTier = 1;
        public float weight = 1f;
        [Tooltip("Só aparece nesses golpes (ex.: os eventos de um golpe extra). Vazio = qualquer golpe a partir do tier mínimo.")]
        public List<HeistDef> onlyInHeists = new List<HeistDef>();

        public bool CanHappenIn(HeistDef heist) =>
            heist != null && minTier <= heist.tier && (onlyInHeists.Count == 0 || onlyInHeists.Exists(h => h != null && h.id == heist.id));

        public List<EventChoice> choices = new List<EventChoice>();
    }
}
