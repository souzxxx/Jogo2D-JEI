using System.Collections.Generic;
using AFirma.Core;
using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Raiz de um prédio (HeistDef.buildingPrefab): as salas em ordem, o caminho da van até a primeira sala,
    /// onde a van estaciona, onde a viatura para e a luz ambiente do prédio.
    /// </summary>
    public class BuildingView : MonoBehaviour
    {
        public string heistId;
        public List<RoomView> rooms = new List<RoomView>();
        [Tooltip("Da porta da van até a entrada da primeira sala (escada e poço inclusos).")]
        public Transform[] entryPath = new Transform[0];
        public Transform vanPoint;
        public Transform policePoint;
        public Color ambient = new Color(0.97f, 0.83f, 0.41f);
        [Range(0f, 1.5f)] public float ambientIntensity = 0.6f;
        [Tooltip("Altura da rua em unidades (0 nos prédios térreos; 4,25 quando o golpe é no subsolo).")]
        public float streetY;
        [Tooltip("Altura do centro da câmera em relação ao piso das salas.")]
        public float cameraY = 3.9f;

        public bool Matches(HeistDef heist) => heist != null && rooms.Count == heist.rooms.Count && !rooms.Contains(null);

        public float MinX
        {
            get
            {
                float x = vanPoint != null ? vanPoint.position.x : transform.position.x;
                if (policePoint != null) x = Mathf.Min(x, policePoint.position.x);
                return x - 3f;
            }
        }

        public float MaxX => transform.position.x + 8f / 32f + rooms.Count * RoomView.WidthUnits + 4f;

        public IEnumerable<Vector3> EntryPoints()
        {
            foreach (var t in entryPath)
                if (t != null) yield return t.position;
        }
    }
}
