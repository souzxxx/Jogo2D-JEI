using System;
using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Fundo de Nova Guanabara à noite: céu parado na câmera e camadas de parallax (morro, prédios de longe e de perto)
    /// que se repetem pros lados. A base das camadas fica na altura da rua do prédio.
    /// </summary>
    public class CityBackdrop : MonoBehaviour
    {
        [Serializable]
        public class Layer
        {
            public Sprite sprite;
            [Range(0f, 1f)] public float parallax = 0.2f;
            [Tooltip("Pixels acima da rua.")]
            public float yOffsetPx;
            public int order = SortOrder.Backdrop;
            [NonSerialized] public SpriteRenderer[] tiles;
        }

        public Camera cam;
        public Sprite sky;
        public Layer[] layers = new Layer[0];
        public float streetY;

        SpriteRenderer skyRenderer;

        void Start() => Build();

        public void SetStreet(float y) => streetY = y;

        void Build()
        {
            if (sky != null)
            {
                skyRenderer = new GameObject("Céu").AddComponent<SpriteRenderer>();
                skyRenderer.transform.SetParent(transform, false);
                skyRenderer.sprite = sky;
                skyRenderer.sortingOrder = SortOrder.Sky;
            }
            foreach (var layer in layers)
            {
                if (layer.sprite == null) continue;
                layer.tiles = new SpriteRenderer[3];
                for (int i = 0; i < 3; i++)
                {
                    var sr = new GameObject(layer.sprite.name + " " + i).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(transform, false);
                    sr.sprite = layer.sprite;
                    sr.sortingOrder = layer.order;
                    layer.tiles[i] = sr;
                }
            }
        }

        void LateUpdate()
        {
            if (cam == null) return;
            var c = cam.transform.position;
            if (skyRenderer != null) skyRenderer.transform.position = Snap(new Vector3(c.x, c.y, 0f));
            foreach (var layer in layers)
            {
                if (layer.tiles == null) continue;
                float w = layer.sprite.bounds.size.x;
                // a camada anda parallax x mais devagar que a câmera e se repete a cada largura do sprite
                float offset = c.x * (1f - layer.parallax);
                float baseX = Mathf.Floor((c.x - offset) / w) * w + offset;
                for (int i = 0; i < 3; i++)
                    layer.tiles[i].transform.position = Snap(new Vector3(baseX + (i - 1) * w, streetY + layer.yOffsetPx / 32f, 0f));
            }
        }

        static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x * 32f) / 32f, Mathf.Round(p.y * 32f) / 32f, 0f);
    }
}
