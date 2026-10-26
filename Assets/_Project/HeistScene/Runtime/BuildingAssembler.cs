using AFirma.Core;
using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Monta um prédio genérico com as salas modulares (um prefab por RoomKind), lado a lado.
    /// Usado quando o HeistDef não tem buildingPrefab ou quando as salas do golpe mudaram e o prefab ficou velho:
    /// o golpe nunca fica sem cena.
    /// </summary>
    public static class BuildingAssembler
    {
        public static BuildingView Assemble(HeistDef heist, HeistSceneArt art, Transform parent)
        {
            var root = new GameObject("Prédio montado: " + heist.displayName);
            root.transform.SetParent(parent, false);
            var building = root.AddComponent<BuildingView>();
            building.heistId = heist.id;

            float wall = 8f / 32f;
            for (int i = 0; i < heist.rooms.Count; i++)
            {
                var prefab = art.roomPrefabs[(int)heist.rooms[i].kind];
                if (prefab == null) prefab = art.roomPrefabs[(int)RoomKind.Corridor];
                var room = Object.Instantiate(prefab, root.transform);
                room.name = $"Sala {i + 1}: {heist.rooms[i].name}";
                room.transform.localPosition = new Vector3(wall + i * RoomView.WidthUnits, 0f, 0f);
                if (room.structure != null)
                {
                    room.structure.SetActive(true);
                    var left = room.structure.transform.Find("ParedeEsquerda");
                    if (left != null) left.gameObject.SetActive(i == 0);
                }
                // a última sala termina na parede de fora: sem porta
                if (i == heist.rooms.Count - 1 && room.exitDoor != null) room.exitDoor.gameObject.SetActive(false);
                building.rooms.Add(room);
            }

            float width = wall + heist.rooms.Count * RoomView.WidthUnits;
            building.vanPoint = Point(root.transform, "Van", new Vector3(-2.2f, 0f, 0f));
            building.policePoint = Point(root.transform, "Viatura", new Vector3(width + 2.8f, 0f, 0f));
            building.entryPath = new[]
            {
                Point(root.transform, "Porta da rua", new Vector3(0.1f, RoomView.FeetY, 0f)),
                Point(root.transform, "Entrada", new Vector3(wall + 0.45f, RoomView.FeetY, 0f)),
            };
            building.ambient = new Color(0.97f, 0.9f, 0.75f);
            building.ambientIntensity = 0.6f;
            return building;
        }

        static Transform Point(Transform parent, string name, Vector3 local)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = local;
            return t;
        }
    }
}
