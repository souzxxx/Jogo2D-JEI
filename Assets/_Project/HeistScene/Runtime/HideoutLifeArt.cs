using System;
using AFirma.Core;
using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Arte do Esconderijo vivo (gerada por Art/Source~/gerar_esconderijo.py e ligada pelo builder).
    /// Tudo desenhado na escala do fundo do esconderijo (640x360).
    /// </summary>
    public class HideoutLifeArt : ScriptableObject
    {
        [Serializable]
        public class CrewLook
        {
            public ThiefType type;
            public Sprite[] idle;
            public Sprite[] celebrate;
        }

        [Header("Crew (quadros tirados do Animator de cada tipo)")]
        public CrewLook[] crew = new CrewLook[0];
        public Sprite emCanaStamp;

        [Header("Bicos nos monitores")]
        public Sprite monitorOn;
        public Sprite monitorOff;
        public Sprite shelf;
        public Sprite neonOn;
        public Sprite neonOff;

        [Header("Upgrades (um objeto por upgrade; listas por nível)")]
        public Sprite[] kombi = new Sprite[0];
        public Sprite[] sleepingBags = new Sprite[0];
        public Sprite[] shoeRacks = new Sprite[0];
        public Sprite pegboard;
        public Sprite[] tools = new Sprite[0];
        public Sprite[] diplomas = new Sprite[0];
        public Sprite phone;
        public Sprite[] rolodex = new Sprite[0];
        [Tooltip("Quadros da máquina de lavar dinheiro (girando e tremendo).")]
        public Sprite[] washer = new Sprite[0];
        [Tooltip("Rádio da polícia: 2 quadros por nível, na ordem nível 1 (a, b), nível 2 (a, b)...")]
        public Sprite[] radio = new Sprite[0];
        public Sprite orelhao;
        public Sprite orelhaoRinging;
        public Sprite note;

        public CrewLook Look(ThiefType type) => Array.Find(crew, c => c != null && c.type == type);

        public static Sprite ByLevel(Sprite[] list, int level) =>
            list == null || list.Length == 0 || level <= 0 ? null : list[Mathf.Clamp(level - 1, 0, list.Length - 1)];
    }
}
