using System.Collections.Generic;
using System.Linq;
using AFirma.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AFirma.HeistScene.EditorTools
{
    public static partial class HeistSceneBuilder
    {
        static readonly string[] KindNames = { "Entrance", "Corridor", "Counter", "CameraRoom", "Office", "Vault", "Tunnel", "Garage" };
        static readonly Dictionary<string, float> NpcFps = new Dictionary<string, float> { { "idle", 2f }, { "andar", 7f }, { "alerta", 5f }, { "distraido", 1.5f } };
        static readonly Dictionary<string, float> ThiefFps = new Dictionary<string, float> { { "idle", 4f }, { "andar", 10f }, { "trabalhar", 8f }, { "comemorar", 8f } };

        static Material particleMaterial;

        public static void BuildPrefabs()
        {
            var l = LoadLayout();
            foreach (var dir in new[] { PrefabDir, PrefabDir + "/Salas", PrefabDir + "/Predios", PrefabDir + "/Personagens", PrefabDir + "/Veiculos", Root + "/VFX" })
                EnsureFolder(dir);

            var art = LoadOrCreate<HeistSceneArt>(Root + "/HeistSceneArt.asset");
            FillArt(art, l);
            var guard = BuildGuardPrefab(l);
            var cam = BuildCameraPrefab();
            art.thiefPrefab = BuildThiefPrefab().GetComponent<ThiefActor>();
            BuildVehiclePrefab("Van", art.vanFrames, false);
            BuildVehiclePrefab("Viatura", art.policeFrames, true);

            for (int k = 0; k < KindNames.Length; k++)
                art.roomPrefabs[k] = BuildRoomPrefab(k, l, art, guard).GetComponent<RoomView>();
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();

            foreach (var b in l.buildings)
            {
                var prefab = BuildBuildingPrefab(b, l, art, guard, cam);
                var heist = AssetDatabase.FindAssets("t:HeistDef", new[] { "Assets/_Project/Data/Heists" })
                    .Select(g => AssetDatabase.LoadAssetAtPath<HeistDef>(AssetDatabase.GUIDToAssetPath(g)))
                    .FirstOrDefault(h => h != null && h.id == b.heistId);
                if (heist == null) continue;
                heist.buildingPrefab = prefab;
                EditorUtility.SetDirty(heist);
            }
            Debug.Log("[Cena do Golpe] Prefabs prontos: 8 salas modulares e " + l.buildings.Length + " prédios.");
        }

        // ---------------------------------------------------------------- arte compartilhada

        static void FillArt(HeistSceneArt art, Layout l)
        {
            art.lockIcon = Sprite("Icones/Icone_Fechadura.png");
            art.systemIcon = Sprite("Icones/Icone_Sistema.png");
            art.personIcon = Sprite("Icones/Icone_Pessoa.png");
            art.checkIcon = Sprite("Icones/Icone_Check.png");
            art.failIcon = Sprite("Icones/Icone_X.png");
            art.alertIcon = Sprite("Icones/Icone_Alerta.png");
            art.doubtIcon = Sprite("Icones/Icone_Duvida.png");
            art.sleepIcon = Sprite("Icones/Icone_Zz.png");
            art.handcuffsIcon = Sprite("Icones/Icone_Algemas.png");
            art.noSpecialistIcon = Sprite("Icones/Icone_SemEspecialista.png");
            art.eventIcon = Sprite("Icones/Icone_Evento.png");
            art.delayIcon = Sprite("Icones/Icone_Relogio.png");
            art.ringFrames = SheetSprites("Icones/Anel.png").OrderBy(p => p.Key).Select(p => p.Value).ToArray();
            art.bagSprites = new[] { Sprite("Icones/Sacola_P.png"), Sprite("Icones/Sacola_M.png"), Sprite("Icones/Sacola_G.png") };
            art.note = Sprite("Icones/Cedula.png");
            art.coin = Sprite("Icones/Moeda.png");
            art.spark = Sprite("Icones/Faisca.png");
            art.dust = Sprite("Icones/Poeira.png");
            art.smoke = Sprite("Icones/Fumaca.png");
            art.splinter = Sprite("Icones/Lasca.png");
            art.bubble = Sprite("Icones/Balao.png");
            art.bubbleTail = Sprite("Icones/Balao_ponta.png");
            art.vanFrames = new[] { Sprite("Rua/Van_0.png"), Sprite("Rua/Van_1.png") };
            art.policeFrames = new[] { Sprite("Rua/Viatura_0.png"), Sprite("Rua/Viatura_1.png") };
            art.lootSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/SFX_Grana.wav");
            art.doorSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/SFX_Porta.wav");
            art.font = BuildFont(l);
            art.genericThief = GenericThiefAnims(l);
        }

        static PixelFont BuildFont(Layout l)
        {
            var font = LoadOrCreate<PixelFont>(ArtDir + "/Fonte/FontePixel.asset");
            var sheet = SheetSprites("Fonte/Fonte_pixel.png");
            var chars = new System.Text.StringBuilder();
            var glyphs = new List<Sprite>();
            var widths = new List<int>();
            foreach (var g in l.font)
            {
                if (g.c == " ") continue;
                sheet.TryGetValue($"glifo_{(int)g.c[0]:X4}", out var sprite);
                if (sprite == null) continue;
                chars.Append(g.c);
                glyphs.Add(sprite);
                widths.Add(g.w);
            }
            font.chars = chars.ToString();
            font.glyphs = glyphs.ToArray();
            font.widths = widths.ToArray();
            font.cellHeight = l.fontCellH;
            font.descent = 2;
            font.spacing = 1;
            font.spaceWidth = 3;
            EditorUtility.SetDirty(font);
            return font;
        }

        static FlipbookAnim[] GenericThiefAnims(Layout l)
        {
            var g = l.genericThief;
            var baseSheet = SheetSprites(g.@base);
            var clothSheet = SheetSprites(g.cloth);
            var anims = new List<FlipbookAnim>();
            foreach (var a in g.anims)
                anims.Add(new FlipbookAnim
                {
                    name = a.name, fps = ThiefFps[a.name], loop = a.name != "comemorar",
                    frames = Frames(baseSheet, "Ladrao_generico_base", a.name, a.frames),
                    overlay = Frames(clothSheet, "Ladrao_generico_roupa", a.name, a.frames),
                });
            return anims.ToArray();
        }

        static FlipbookAnim[] NpcAnims(Layout l, string variant)
        {
            var npc = l.npcs.First(n => n.name == variant);
            var sheet = SheetSprites(npc.sheet);
            return npc.anims.Select(a => new FlipbookAnim
            {
                name = a.name, fps = NpcFps[a.name], loop = true,
                frames = Frames(sheet, "NPC_" + variant, a.name, a.frames),
            }).ToArray();
        }

        // ---------------------------------------------------------------- peças

        static GameObject Child(Transform parent, string name, Vector3 local)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            return go;
        }

        static SpriteRenderer SpriteChild(Transform parent, string name, Sprite sprite, Vector3 local, int order)
        {
            var sr = Child(parent, name, local).AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        static Light2D PointLight(Transform parent, string name, Vector3 local, Color color, float radius, float intensity,
                                  float outerAngle = 360f, float innerAngle = 360f, float rotation = 0f)
        {
            var go = Child(parent, name, local);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * 0.15f;
            light.pointLightOuterAngle = outerAngle;
            light.pointLightInnerAngle = innerAngle;
            light.falloffIntensity = 0.65f;
            light.targetSortingLayers = SortingLayer.layers.Select(s => s.id).ToArray();
            return light;
        }

        static Material ParticleMaterial()
        {
            if (particleMaterial != null) return particleMaterial;
            const string path = Root + "/VFX/M_Particulas.mat";
            particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (particleMaterial == null)
            {
                EnsureFolder(Root + "/VFX");
                particleMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(particleMaterial, path);
            }
            return particleMaterial;
        }

        static ParticleSystem Particles(Transform parent, string name, Sprite sprite, Color start, Color end, int order)
        {
            var go = Child(parent, name, Vector3.zero);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = start;
            // partícula de sprite: o tamanho é em unidades, então 1 px do sprite = 1/32
            main.startSize = sprite != null ? sprite.rect.width / Ppu : 0.1f;
            main.scalingMode = ParticleSystemScalingMode.Local;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.SetSprite(0, sprite);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = ParticleMaterial();
            renderer.sortingOrder = order;
            main.startColor = new ParticleSystem.MinMaxGradient(start, end);
            return ps;
        }

        /// <summary>Explosão única (toca ao nascer).</summary>
        static ParticleSystem Burst(Transform parent, string name, Sprite sprite, int count, float minSpeed, float maxSpeed,
                                    float minLife, float maxLife, float gravity, Color start, Color end)
        {
            var ps = Particles(parent, name, sprite, start, end, SortOrder.Fx);
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = false;
            main.duration = 0.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.gravityModifier = gravity;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;
            return ps;
        }

        /// <summary>Emissor contínuo (poeira do túnel, fumaça do escapamento).</summary>
        static ParticleSystem Emitter(Transform parent, string name, Sprite sprite, float rate, float speed, float life,
                                      float gravity, Color start, Color end, Vector2 box, bool playOnAwake)
        {
            var ps = Particles(parent, name, sprite, start, end, SortOrder.Fx - 2);
            var main = ps.main;
            main.playOnAwake = playOnAwake;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.gravityModifier = gravity;
            main.maxParticles = 80;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(box.x, box.y, 0.1f);
            if (!playOnAwake) ps.Stop();
            return ps;
        }

        static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------------------------------------------------------------- personagens e veículos

        static GameObject BuildGuardPrefab(Layout l)
        {
            var root = new GameObject("Guarda");
            var body = root.AddComponent<SpriteRenderer>();
            body.sortingOrder = SortOrder.Npc;
            var flip = root.AddComponent<SpriteFlipbook>();
            flip.body = body;
            flip.anims = NpcAnims(l, "seguranca");
            body.sprite = flip.anims[0].frames[0];
            var guard = root.AddComponent<GuardView>();
            guard.flipbook = flip;
            guard.mark = SpriteChild(root.transform, "Marca", null, Px(0, 33), SortOrder.Badge - 1);
            guard.mark.enabled = false;
            guard.flashlight = PointLight(root.transform, "Lanterna", Px(8, 13), new Color(1f, 0.95f, 0.8f), 3.4f, 0.8f, 55f, 25f, -90f);
            guard.cone = SpriteChild(root.transform, "Cone", Sprite("Icones/Cone.png"), Px(9, 13), SortOrder.Npc - 1);
            guard.cone.color = new Color(1f, 0.95f, 0.8f, 0.35f);
            guard.doubtIcon = Sprite("Icones/Icone_Duvida.png");
            guard.alertIcon = Sprite("Icones/Icone_Alerta.png");
            guard.sleepIcon = Sprite("Icones/Icone_Zz.png");
            return SavePrefab(root, PrefabDir + "/Personagens/Guarda.prefab");
        }

        static GameObject BuildCameraPrefab()
        {
            var root = new GameObject("CameraSeguranca");
            var view = root.AddComponent<SecurityCameraView>();
            view.body = SpriteChild(root.transform, "Corpo", Sprite("Icones/Camera.png"), Vector3.zero, SortOrder.Prop + 2);
            view.pivot = Child(root.transform, "Pivô", Px(5, -1)).transform;
            view.cone = SpriteChild(view.pivot, "Cone", Sprite("Icones/Cone.png"), Px(2, 0), SortOrder.Prop + 1);
            view.coneLight = PointLight(view.pivot, "Luz", Px(2, 0), new Color(0.35f, 0.66f, 0.39f), 3f, 0.7f, 40f, 15f, -90f);
            return SavePrefab(root, PrefabDir + "/Personagens/CameraSeguranca.prefab");
        }

        static GameObject BuildThiefPrefab()
        {
            var root = new GameObject("Ladrao");
            var body = root.AddComponent<SpriteRenderer>();
            body.sortingOrder = SortOrder.Thief;
            var animator = root.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var flip = root.AddComponent<SpriteFlipbook>();
            flip.body = body;
            var actor = root.AddComponent<ThiefActor>();
            actor.body = body;
            actor.animator = animator;
            actor.flipbook = flip;
            actor.cloth = SpriteChild(root.transform, "Roupa", null, Vector3.zero, SortOrder.Thief + 1);
            flip.overlay = actor.cloth;
            actor.bag = SpriteChild(root.transform, "Sacola", null, Px(-7, 9), SortOrder.Thief + 2);
            actor.marker = SpriteChild(root.transform, "Marcador", null, Px(0, 36), SortOrder.Badge);
            return SavePrefab(root, PrefabDir + "/Personagens/Ladrao.prefab");
        }

        static GameObject BuildVehiclePrefab(string name, Sprite[] frames, bool police)
        {
            var root = new GameObject(name);
            var view = root.AddComponent<VehicleView>();
            view.sr = root.AddComponent<SpriteRenderer>();
            view.sr.sprite = frames[0];
            view.sr.sortingOrder = SortOrder.Vehicle;
            view.frames = frames;
            view.exhaust = Emitter(root.transform, "Escapamento", Sprite("Icones/Fumaca.png"), 14f, 0.6f, 0.9f, -0.15f,
                                   new Color(0.8f, 0.82f, 0.88f, 0.8f), new Color(0.55f, 0.6f, 0.7f, 0.6f), new Vector2(0.1f, 0.1f), false);
            view.exhaust.transform.localPosition = Px(-30, 6);
            if (police)
            {
                view.flashers = new[]
                {
                    PointLight(root.transform, "Giroflex vermelho", Px(-3, 27), new Color(1f, 0.2f, 0.2f), 3.2f, 1.6f),
                    PointLight(root.transform, "Giroflex azul", Px(3, 27), new Color(0.25f, 0.45f, 1f), 3.2f, 1.6f),
                };
                foreach (var f in view.flashers) f.enabled = false;
            }
            else
            {
                PointLight(root.transform, "Farol", Px(28, 9), new Color(1f, 0.9f, 0.6f), 2.2f, 0.7f, 70f, 30f, -90f);
            }
            return SavePrefab(root, PrefabDir + "/Veiculos/" + name + ".prefab");
        }

        // ---------------------------------------------------------------- salas modulares

        static string DefaultLockProp(string kind) =>
            kind == "Vault" ? "Cofre" : kind == "Office" ? "Arquivo" : kind == "Tunnel" ? "GradeEnferrujada" : "Grade";

        static string DefaultSystemProp(string kind) =>
            kind == "CameraRoom" ? "Cftv" : kind == "Vault" ? "CofreEletronico" : kind == "Tunnel" ? "Sensor" : kind == "Office" ? "Terminal" : "Painel";

        /// <summary>(x do centro, y da base, x de trabalho) do adereço na sala, em px. Igual ao prop_geometry do gerar_cena.py.</summary>
        static (float x, float y, float work) PropGeometry(string prop)
        {
            if (prop.StartsWith("npc:")) return (150, 3, 124);
            switch (prop)
            {
                case "Cofre":
                case "CofreEletronico": return (150, 22, 128);
                case "CofreCentral": return (140, 12, 112);
                case "Cftv": return (140, 30, 128);
                case "Painel":
                case "Sensor": return (150, 40, 138);
                case "Terminal": return (152, 22, 130);
                case "Cofrinho": return (156, 4, 136);
                case "Arquivo": return (154, 4, 134);
                default: return (156, 4, 132);
            }
        }

        static ObstacleProp MakeProp(Transform room, ObstacleType obstacle, string prop, Layout l)
        {
            var (x, y, work) = PropGeometry(prop);
            var go = Child(room, "Obstaculo_" + (obstacle == ObstacleType.Lock ? "Fechadura" : obstacle == ObstacleType.System ? "Sistema" : "Pessoa"), Px(x, y));
            var p = go.AddComponent<ObstacleProp>();
            p.obstacle = obstacle;
            p.workPoint = Child(go.transform, "Trabalho", Px(work - x, 3 - y)).transform;
            ConfigureProp(p, prop, l);
            return p;
        }

        static void ConfigureProp(ObstacleProp p, string prop, Layout l)
        {
            var info = l.props.FirstOrDefault(i => i.name == prop);
            if (info == null) return;
            if (p.sr == null) p.sr = p.gameObject.GetComponent<SpriteRenderer>();
            if (p.sr == null) p.sr = p.gameObject.AddComponent<SpriteRenderer>();
            p.closed = Sprite(info.closed);
            p.open = Sprite(info.open);
            p.sr.sprite = p.closed;
            p.sr.sortingOrder = SortOrder.Prop;
        }

        static GameObject BuildRoomPrefab(int kindIndex, Layout l, HeistSceneArt art, GameObject guardPrefab)
        {
            string kind = KindNames[kindIndex];
            var info = l.defaults.First(d => d.kind == kindIndex);
            var root = new GameObject("Sala_" + kind);
            var room = root.AddComponent<RoomView>();
            room.kind = (RoomKind)kindIndex;
            room.background = SpriteChild(root.transform, "Fundo", Sprite(info.background), Vector3.zero, SortOrder.Room);

            // estrutura genérica (só no prédio montado sem prefab próprio)
            var structure = Child(root.transform, "Estrutura", Vector3.zero);
            SpriteChild(structure.transform, "Parede", Sprite("Predios/Modulo_parede.png"), Px(192, 0), SortOrder.Shell + 1);
            SpriteChild(structure.transform, "ParedeEsquerda", Sprite("Predios/Modulo_parede.png"), Px(-8, 0), SortOrder.Shell + 1).gameObject.SetActive(false);
            SpriteChild(structure.transform, "Laje", Sprite("Predios/Modulo_laje.png"), Px(-8, 96), SortOrder.Shell);
            SpriteChild(structure.transform, "Piso", Sprite("Predios/Modulo_piso.png"), Px(-8, -8), SortOrder.Shell);
            SpriteChild(structure.transform, "Fachada", Sprite("Predios/Modulo_fachada.png"), Px(-8, 104), SortOrder.Shell);
            SpriteChild(structure.transform, "Fundacao", Sprite("Predios/Modulo_fundacao.png"), Px(-8, -56), SortOrder.Shell);
            structure.SetActive(false);
            room.structure = structure;

            // porta da direita
            var doorGo = Child(root.transform, "Porta", Px(192, 0));
            var door = doorGo.AddComponent<DoorView>();
            door.sr = doorGo.AddComponent<SpriteRenderer>();
            door.sr.sortingOrder = SortOrder.RoomDecor + 1;
            var doorInfo = l.doors.First(d => d.style == info.door);
            door.closed = Sprite(doorInfo.closed);
            door.open = Sprite(doorInfo.open);
            door.sr.sprite = door.closed;
            door.highlight = SpriteChild(doorGo.transform, "Destaque", Sprite("Icones/Porta_destaque.png"), Px(-2, -2), SortOrder.Badge - 2);
            door.highlight.enabled = false;
            room.exitDoor = door;

            room.entryPoint = Child(root.transform, "Entrada", Px(14, 3)).transform;
            room.exitPoint = Child(root.transform, "Saida", Px(186, 3)).transform;

            // um adereço por tipo de obstáculo: a sala liga o da RoomData
            var lockProp = MakeProp(root.transform, ObstacleType.Lock, DefaultLockProp(kind), l);
            var systemProp = MakeProp(root.transform, ObstacleType.System, DefaultSystemProp(kind), l);
            var personProp = MakeProp(root.transform, ObstacleType.Person, "npc:seguranca", l);
            var person = ((GameObject)PrefabUtility.InstantiatePrefab(guardPrefab, personProp.transform)).GetComponent<GuardView>();
            person.transform.localPosition = Vector3.zero;
            person.mode = GuardView.Mode.Post;
            person.flashlight.gameObject.SetActive(false);
            person.cone.gameObject.SetActive(false);
            personProp.person = person;
            room.props = new[] { lockProp, systemProp, personProp };

            // ícone do obstáculo com anel de progresso
            var badgeGo = Child(root.transform, "Placa", Px(150, 72));
            var badge = badgeGo.AddComponent<ObstacleBadge>();
            badge.plate = SpriteChild(badgeGo.transform, "Fundo", Sprite("Icones/Placa.png"), Vector3.zero, SortOrder.Badge);
            badge.ring = SpriteChild(badgeGo.transform, "Anel", art.ringFrames.Length > 0 ? art.ringFrames[0] : null, Vector3.zero, SortOrder.Badge + 1);
            badge.icon = SpriteChild(badgeGo.transform, "Icone", art.lockIcon, Vector3.zero, SortOrder.Badge + 2);
            badge.warning = SpriteChild(badgeGo.transform, "SemEspecialista", art.noSpecialistIcon, Px(11, 9), SortOrder.Badge + 3);
            badge.warning.enabled = false;
            badge.ringFrames = art.ringFrames;
            badge.checkIcon = art.checkIcon;
            room.badge = badge;

            // luz da sala e giroflex
            var light = PointLight(root.transform, "Luz", Px(96, 80), info.lightColor.Color, 5f, 0.85f);
            room.lights = new[] { light };
            var alarmGo = Child(root.transform, "Alarme", Px(60, 90));
            var alarm = alarmGo.AddComponent<AlarmLight>();
            alarm.beacon = SpriteChild(alarmGo.transform, "Giroflex", Sprite("Icones/Giroflex.png"), Vector3.zero, SortOrder.RoomDecor + 2);
            alarm.beam = PointLight(alarmGo.transform, "Feixe", Vector3.zero, new Color(1f, 0.15f, 0.12f), 5.5f, 1.3f, 50f, 20f);
            alarm.beam.enabled = false;
            room.alarm = alarm;

            // sala escura até a crew chegar
            room.fog = SpriteChild(root.transform, "Neblina", Sprite("Icones/Branco.png"), Vector3.zero, SortOrder.Fog);
            room.fog.transform.localScale = new Vector3(192f / 4f, 96f / 4f, 1f);
            room.fog.color = new Color(0.04f, 0.035f, 0.07f, 0.82f);

            if (kind == "Tunnel")
            {
                room.dust = Emitter(root.transform, "Poeira", Sprite("Icones/Poeira.png"), 6f, 0.15f, 3f, 0.05f,
                                    new Color(0.81f, 0.62f, 0.39f, 0.8f), new Color(0.63f, 0.43f, 0.26f, 0.6f), new Vector2(5.8f, 2.6f), false);
                room.dust.transform.localPosition = Px(96, 50);
            }
            return SavePrefab(root, PrefabDir + "/Salas/Sala_" + kind + ".prefab");
        }

        // ---------------------------------------------------------------- prédios

        static GameObject BuildBuildingPrefab(BuildingInfo b, Layout l, HeistSceneArt art, GameObject guardPrefab, GameObject cameraPrefab)
        {
            var root = new GameObject("Predio_" + b.heistId);
            var building = root.AddComponent<BuildingView>();
            building.heistId = b.heistId;
            building.ambient = b.ambient.Color;
            building.ambientIntensity = b.ambientIntensity;
            building.streetY = b.streetY / Ppu;
            building.cameraY = b.cameraY / Ppu;

            SpriteChild(root.transform, "Casca", Sprite(b.shell), Px(b.shellX, b.shellY), SortOrder.Shell);

            var street = Child(root.transform, "Rua", Vector3.zero).transform;
            foreach (var p in b.props)
            {
                var sprite = Sprite(p.sprite);
                if (sprite == null) continue;
                int order = p.order != 0 ? p.order : SortOrder.Street;
                var sr = SpriteChild(street, System.IO.Path.GetFileNameWithoutExtension(p.sprite), sprite, Px(p.x, p.y), order);
                if (p.light)
                    PointLight(sr.transform, "Luz do poste", Px(14, 80), new Color(0.97f, 0.83f, 0.41f), 4.5f, 0.9f, 80f, 40f, 180f);
            }

            var rooms = Child(root.transform, "Salas", Vector3.zero).transform;
            for (int i = 0; i < b.rooms.Length; i++)
                building.rooms.Add(BuildBuildingRoom(b, b.rooms[i], i == b.rooms.Length - 1, rooms, l, art, guardPrefab, cameraPrefab));

            var path = Child(root.transform, "Caminho da van", Vector3.zero).transform;
            building.entryPath = b.entryPath.Select((p, i) => Child(path, "Ponto " + (i + 1), Px(p.x, p.y)).transform).ToArray();
            building.vanPoint = Child(root.transform, "Van", Px(b.van.x, b.van.y)).transform;
            building.policePoint = Child(root.transform, "Viatura", Px(b.police.x, b.police.y)).transform;

            return SavePrefab(root, PrefabDir + "/Predios/Predio_" + b.heistId + ".prefab");
        }

        static RoomView BuildBuildingRoom(BuildingInfo b, RoomInfo r, bool last, Transform parent, Layout l, HeistSceneArt art,
                                          GameObject guardPrefab, GameObject cameraPrefab)
        {
            var prefab = art.roomPrefabs[r.kind].gameObject;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = $"Sala {r.index + 1} - {r.name}";
            go.transform.localPosition = Px(r.x, 0);
            var room = go.GetComponent<RoomView>();
            room.background.sprite = Sprite(r.background);
            room.entryPoint.localPosition = Px(r.entryX - r.x, 3);
            room.exitPoint.localPosition = Px(r.exitX - r.x, 3);

            // porta: estilo do tema; a última sala acaba na parede de fora
            if (last || string.IsNullOrEmpty(r.door)) room.exitDoor.gameObject.SetActive(false);
            else
            {
                var d = l.doors.First(x => x.style == r.door);
                room.exitDoor.closed = Sprite(d.closed);
                room.exitDoor.open = Sprite(d.open);
                room.exitDoor.sr.sprite = room.exitDoor.closed;
            }

            // adereço do obstáculo desta sala
            var obstacle = (ObstacleType)r.obstacle;
            var prop = room.props.First(p => p.obstacle == obstacle);
            prop.transform.localPosition = Px(r.propX - r.x, r.propY);
            prop.workPoint.localPosition = Px(r.workX - r.propX, 3 - r.propY);
            if (r.prop.StartsWith("npc:"))
            {
                var anims = NpcAnims(l, r.prop.Substring(4));
                prop.person.flipbook.anims = anims;
                prop.person.flipbook.body.sprite = anims[0].frames[0];
            }
            else ConfigureProp(prop, r.prop, l);

            room.badge.transform.localPosition = Px(r.iconX - r.x, r.iconY);

            // luzes do tema
            var lights = new List<Light2D>();
            for (int i = 0; i < r.lights.Length; i++)
            {
                var info = r.lights[i];
                var light = i == 0 ? room.lights[0] : PointLight(go.transform, "Luz " + (i + 1), Vector3.zero, Color.white, 1f, 1f);
                light.transform.localPosition = Px(info.x - r.x, info.y);
                light.color = Rgb(info.color);
                light.pointLightOuterRadius = info.radius;
                light.pointLightInnerRadius = info.radius * 0.15f;
                light.intensity = info.intensity;
                lights.Add(light);
            }
            room.lights = lights.ToArray();
            room.alarm.transform.localPosition = Px(r.alarmX - r.x, r.alarmY);

            // guardas de ronda e câmeras de segurança
            var guards = new List<GuardView>();
            foreach (var g in r.guards)
            {
                var guard = ((GameObject)PrefabUtility.InstantiatePrefab(guardPrefab, go.transform)).GetComponent<GuardView>();
                guard.name = "Guarda " + g.variant;
                guard.transform.localPosition = Px((g.minX + g.maxX) / 2f - r.x, 3);
                guard.minX = (g.minX - r.x) / Ppu;
                guard.maxX = (g.maxX - r.x) / Ppu;
                guard.mode = GuardView.Mode.Patrol;
                guard.flipbook.anims = NpcAnims(l, g.variant);
                guard.flipbook.body.sprite = guard.flipbook.anims[0].frames[0];
                guards.Add(guard);
            }
            room.guards = guards.ToArray();
            var cams = new List<SecurityCameraView>();
            foreach (var c in r.cameras)
            {
                var cam = ((GameObject)PrefabUtility.InstantiatePrefab(cameraPrefab, go.transform)).GetComponent<SecurityCameraView>();
                cam.transform.localPosition = Px(c.x - r.x, c.y);
                if (c.facing < 0) cam.transform.localScale = new Vector3(-1f, 1f, 1f);
                cams.Add(cam);
            }
            room.cameras = cams.ToArray();
            return room;
        }
    }
}
