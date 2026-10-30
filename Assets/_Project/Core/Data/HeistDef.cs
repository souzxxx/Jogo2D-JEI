using System;
using System.Collections.Generic;
using UnityEngine;

namespace AFirma.Core
{
    [Serializable]
    public class RoomData
    {
        public string name;
        public RoomKind kind;
        public ObstacleType obstacle;
        [Tooltip("Quanto maior, mais tempo pra limpar. Tempo ≈ dificuldade / poder.")]
        public float difficulty = 100f;
        public double loot = 100;
        [Tooltip("Alerta ganho por segundo enquanto a crew trabalha nessa sala.")]
        public float alertPerSecond = 2f;
    }

    [CreateAssetMenu(menuName = "A Firma/Golpe", fileName = "Heist")]
    public class HeistDef : DefBase
    {
        [Tooltip("Posição na escada de golpes (1 = Lotérica).")]
        public int tier = 1;
        [TextArea(2, 5)] public string briefing;

        [Header("Liberação")]
        public bool unlockedByDefault;
        [Tooltip("Golpe extra: ramo ao lado da escada. Não é caminho pro Banco Central e não conta pro prestígio.")]
        public bool extra;
        [Tooltip("Golpe que precisa ser concluído (cofre limpo) pra liberar este.")]
        public HeistDef unlockedBy;

        [Header("Salas em ordem. A última é o cofre principal.")]
        public List<RoomData> rooms = new List<RoomData>();

        [Header("Prisão")]
        [Tooltip("Tempo base de prisão de quem cai neste golpe, em segundos.")]
        public float jailSeconds = 120f;

        [Header("Mapa e cena")]
        public Vector2 mapPosition;
        [Tooltip("Prefab do prédio montado pela Cena do Golpe. Opcional: sem ele a cena monta as salas pelos RoomKind.")]
        public GameObject buildingPrefab;

        public double TotalLoot
        {
            get
            {
                double total = 0;
                foreach (var room in rooms) total += room.loot;
                return total;
            }
        }
    }
}
