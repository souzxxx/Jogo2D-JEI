using System;
using System.Collections.Generic;
using System.IO;
using AFirma.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AFirma.EditorTools
{
    /// <summary>
    /// Cria os dados de exemplo, as cenas e as configurações de build da fundação.
    /// Só cria o que está faltando: rodar de novo não sobrescreve nada.
    /// Menu: A Firma > Setup > Rodar setup da fundação.
    /// </summary>
    public static class FoundationSetup
    {
        const string Root = "Assets/_Project";
        const string Data = Root + "/Data";
        const string SceneTemplate = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";

        [MenuItem("A Firma/Setup/Rodar setup da fundação")]
        public static void RunAll()
        {
            CreateOwnerFolders();
            var db = CreateData();
            CreateScenes(db);
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[A Firma] Setup da fundação concluído.");
        }

        // ------------------------------------------------------------------ pastas

        static readonly (string path, string text)[] OwnerFolders =
        {
            (Root + "/Heist", "Motor do Golpe (Leonardo): HeistRunner implementando IHeistService."),
            (Root + "/Crew", "Crew (Leonardo): CrewService implementando ICrewService."),
            (Root + "/Economy", "Economia (Nicolas): EconomyService implementando IEconomyService."),
            (Root + "/Progression", "Progressão (Nicolas): ProgressionService implementando IProgressionService."),
            (Root + "/Narrative", "Narrativa (Nicolas): bips da Dona Cida, briefings, manchetes."),
            (Root + "/UI", "Interface (Enzo): menus, telas, HUD, popups, tutorial."),
            (Root + "/Audio", "Áudio (Enzo): AudioService, mixer, músicas e SFX."),
            (Root + "/HeistScene", "Cena do Golpe (Miguel): salas, prédios, crew na cena, guardas, luzes, VFX."),
            (Root + "/Thieves/PeNaTabua", "Pé-na-Tábua (Leonardo): sprites, animações, retrato, habilidade, eventos, bico."),
            (Root + "/Thieves/Gazua", "Gazua (Miguel): sprites, animações, retrato, habilidade, eventos, bico."),
            (Root + "/Thieves/Disquete", "Disquete (Enzo): sprites, animações, retrato, habilidade, eventos, bico."),
            (Root + "/Thieves/171", "171 (Nicolas): sprites, animações, retrato, habilidade, eventos, bico."),
            ("Assets/ThirdParty", "Packs de terceiros (craftpix etc.). Todo pack aqui precisa estar em docs/ASSETS.md e nos créditos do README."),
        };

        static void CreateOwnerFolders()
        {
            foreach (var (path, text) in OwnerFolders)
            {
                EnsureFolder(path);
                var readme = path + "/LEIA-ME.md";
                if (!File.Exists(readme)) File.WriteAllText(readme, "# " + Path.GetFileName(path) + "\n\n" + text + "\n");
            }
        }

        // ------------------------------------------------------------------ dados

        static GameDatabase CreateData()
        {
            var db = Create<GameDatabase>(Data + "/Resources/GameDatabase.asset", _ => { });
            db.config = Create<GameConfig>(Data + "/Config/GameConfig.asset", _ => { });

            db.names = Create<NamePoolDef>(Data + "/Crew/NamePool.asset", n => n.firstNames = new[]
            {
                "Juninho", "Tião", "Zé Carlos", "Marquinhos", "Dinho", "Paulinho", "Beto", "Nando", "Toninho",
                "Celinha", "Rosinha", "Vavá", "Lucinha", "Marcinho", "Biel", "Cris", "Duda", "Fabinho",
                "Kelly", "Sheila", "Jaque", "Robson", "Wellington", "Cleiton", "Gilmar"
            });

            db.headlines = Create<HeadlineSetDef>(Data + "/Narrative/Headlines.asset", h =>
            {
                h.clean = new[] { "{golpe}: QUADRILHA LEVA {valor} SEM DEIXAR RASTRO", "MISTÉRIO EM NOVA GUANABARA: SOMEM {valor} E NINGUÉM VIU NADA" };
                h.hasty = new[] { "FUGA ÀS PRESSAS! BANDO ESCAPA COM {valor} ANTES DA POLÍCIA CHEGAR" };
                h.forced = new[] { "{golpe}: POLÍCIA CHEGA E BANDO FOGE DEIXANDO DINHEIRO PRA TRÁS" };
                h.perfect = new[] { "CAVALO DE PAU HISTÓRICO: VAN SOME COM {valor} NA FRENTE DA POLÍCIA" };
                h.withArrests = new[] { "DELEGADO BASTOS PRENDE INTEGRANTE DE QUADRILHA: \"É SÓ O COMEÇO\"" };
                h.interrupted = new[] { "{golpe}: QUADRILHA ABANDONA O SERVIÇO PELA METADE E SOME" };
            });

            // Bicos
            var latinha = Create<SideJobDef>(Data + "/Economy/SideJobs/SideJob_CatarLatinha.asset", j =>
            {
                Def(j, "catar-latinha", "Catar latinha", "Renda mínima que nunca para. Ninguém precisa ir.");
                j.requiresThief = false; j.hasPreferredType = false; j.incomePerSecond = 1;
            });

            // Tipos de ladrão (cada um na pasta do dono)
            var driver = MakeThiefType(ThiefTypeAsset("PeNaTabua"), "pe-na-tabua", "Pé-na-Tábua", ThiefType.Driver,
                "Motorista. Não abre nada, mas sem ele a fuga é no desespero: dá mais tempo antes da polícia chegar e segura o loot e a crew na fuga.",
                false, ObstacleType.Lock, 25, 30, 40,
                new[] { "Segura que eu dirijo!", "A van tá ligada, chefia.", "Conheço um atalho." },
                new Color(0.95f, 0.65f, 0.2f));
            driver.policeCountdownBonus = 4f; driver.escapeLootBonus = 0.1f; driver.arrestChanceReduction = 0.15f;
            driver.ability = Ability("PeNaTabua", "cavalo-de-pau", "Cavalo de pau",
                "Fuga perfeita a qualquer hora, até com a polícia na porta: leva 100% da sacola e ninguém é preso.",
                a => a.perfectEscape = true);
            driver.signatureSideJob = SideJob("PeNaTabua", "lotacao-pirata", "Lotação pirata", "Rende o dobro com um Pé-na-Tábua.", ThiefType.Driver);

            var locksmith = MakeThiefType(ThiefTypeAsset("Gazua"), "gazua", "Gazua", ThiefType.Locksmith,
                "Arrombador. Especialista em Fechadura: portas, cadeados e cofres.",
                true, ObstacleType.Lock, 35, 30, 30,
                new[] { "Essa fechadura é de brinquedo.", "Dá licença que eu abro.", "Cadeado vagabundo, moleza." },
                new Color(0.75f, 0.75f, 0.8f));
            locksmith.ability = Ability("Gazua", "pe-de-cabra", "Pé-de-cabra",
                "Limpa na hora uma sala de Fechadura, mas faz barulho: +15 de alerta.",
                a => { a.completeCurrentRoom = true; a.requireRoomObstacle = true; a.roomObstacle = ObstacleType.Lock; a.alertDelta = 15f; });
            locksmith.signatureSideJob = SideJob("Gazua", "chaveiro-24h", "Chaveiro 24h", "Rende o dobro com um Gazua.", ThiefType.Locksmith);

            var hacker = MakeThiefType(ThiefTypeAsset("Disquete"), "disquete", "Disquete", ThiefType.Hacker,
                "Hacker. Especialista em Sistema: câmeras, alarmes e computadores.",
                true, ObstacleType.System, 35, 35, 25,
                new[] { "Entrei no sistema.", "Isso aqui roda Windows 95, relaxa.", "Câmera em loop, pode passar." },
                new Color(0.3f, 0.85f, 0.5f));
            hacker.ability = Ability("Disquete", "loop-na-camera", "Loop na câmera",
                "Congela o alerta por 15 segundos.",
                a => a.freezeAlertSeconds = 15f);
            hacker.signatureSideJob = SideJob("Disquete", "gatonet", "Gatonet", "Rende o dobro com um Disquete.", ThiefType.Hacker);

            var conArtist = MakeThiefType(ThiefTypeAsset("171"), "171", "171", ThiefType.ConArtist,
                "Vigarista. Especialista em Pessoa: guardas, caixas e curiosos. Lábia pra baixar o alerta.",
                true, ObstacleType.Person, 35, 30, 35,
                new[] { "Deixa que eu converso.", "Boa noite, sou da companhia de luz.", "Confia, chefia." },
                new Color(0.85f, 0.3f, 0.35f));
            conArtist.ability = Ability("171", "labia", "Lábia",
                "Conversa mole na hora certa: -25 de alerta.",
                a => a.alertDelta = -25f);
            conArtist.signatureSideJob = SideJob("171", "bilhete-premiado", "Bilhete premiado", "Rende o dobro com um 171.", ThiefType.ConArtist);

            foreach (var t in new[] { driver, locksmith, hacker, conArtist }) EditorUtility.SetDirty(t);
            db.thiefTypes = new List<ThiefTypeDef> { driver, locksmith, hacker, conArtist };
            db.sideJobs = new List<SideJobDef> { latinha, driver.signatureSideJob, locksmith.signatureSideJob, hacker.signatureSideJob, conArtist.signatureSideJob };

            db.traits = new List<TraitDef>
            {
                Trait("fominha", "Fominha", "Pega mais do que devia: +20% de loot, -15% de Discrição.", m => { m.lootMult = 1.2f; m.stealthMult = 0.85f; }),
                Trait("pe-frio", "Pé-frio", "Atrai confusão: eventos ruins acontecem mais.", m => m.badEventChanceMult = 1.5f),
                Trait("sangue-frio", "Sangue-frio", "Não treme: +30% de Nervos.", m => m.nervesMult = 1.3f),
                Trait("boca-aberta", "Boca-aberta", "Fala alto demais: +20% de alerta.", m => m.alertMult = 1.2f),
                Trait("pao-duro", "Pão-duro", "Negocia até a fiança: -40% no preço.", m => m.bailMult = 0.6f),
                Trait("cria-da-rua", "Cria da rua", "Aprende rápido: +30% de XP.", m => m.xpMult = 1.3f),
                Trait("supersticioso", "Supersticioso", "Reza antes de tudo: +20% de Nervos, -10% de Perícia.", m => { m.nervesMult = 1.2f; m.skillMult = 0.9f; }),
                Trait("mao-boba", "Mão-boba", "Carteira alheia é com ele: +30% de loot em salas de Pessoa.", m => m.personRoomLootMult = 1.3f),
            };

            db.heists = CreateHeists();
            db.events = CreateEvents();
            db.upgrades = CreateUpgrades();
            db.modifiers = CreateModifiers();
            db.famePerks = CreateFamePerks();

            EditorUtility.SetDirty(db);
            return db;
        }

        static List<HeistDef> CreateHeists()
        {
            var loterica = Heist("loterica", "Lotérica do Seu Zé", 1, null, 60f,
                "O Seu Zé guarda o dinheiro das apostas num cofrinho atrás do balcão. Dona Cida diz que é o golpe perfeito pra começar: pouca gente e nenhuma câmera.",
                Room("Entrada", RoomKind.Entrance, ObstacleType.Person, 250, 50, 1.5f),
                Room("Balcão", RoomKind.Counter, ObstacleType.Person, 300, 150, 2f),
                Room("Cofrinho", RoomKind.Vault, ObstacleType.Lock, 350, 300, 2.5f));
            loterica.unlockedByDefault = true;

            var joalheria = Heist("joalheria", "Joalheria Brilho Eterno", 2, loterica, 120f,
                "Vitrine cheia de ouro e um alarme novinho em folha. Vai precisar de alguém que entenda de computador.",
                Room("Vitrine", RoomKind.Entrance, ObstacleType.Person, 450, 300, 2f),
                Room("Sala de Câmeras", RoomKind.CameraRoom, ObstacleType.System, 550, 400, 3f),
                Room("Escritório", RoomKind.Office, ObstacleType.Lock, 600, 800, 3f),
                Room("Cofre", RoomKind.Vault, ObstacleType.System, 700, 1500, 4f));

            var cambio = Heist("cambio", "Casa de Câmbio Dólar Fácil", 3, joalheria, 180f,
                "Dólar, marco alemão e cruzeiro velho. O dono não confia em banco, então tá tudo lá dentro.",
                Room("Fachada", RoomKind.Entrance, ObstacleType.Lock, 800, 1500, 3f),
                Room("Balcão", RoomKind.Counter, ObstacleType.Person, 900, 2500, 3.5f),
                Room("Corredor", RoomKind.Corridor, ObstacleType.System, 950, 3000, 4f),
                Room("Escritório do Gerente", RoomKind.Office, ObstacleType.Person, 1050, 4000, 4f),
                Room("Cofre", RoomKind.Vault, ObstacleType.Lock, 1200, 7000, 5f));

            var cassino = Heist("cassino", "Cassino Clandestino", 4, cambio, 240f,
                "Debaixo de uma pastelaria da Zona Norte funciona o cassino mais animado da cidade. Muito segurança, muito dinheiro vivo, e ninguém vai chamar a polícia… em tese.",
                Room("Pastelaria", RoomKind.Entrance, ObstacleType.Person, 1400, 5000, 4f),
                Room("Corredor", RoomKind.Corridor, ObstacleType.Lock, 1500, 8000, 4.5f),
                Room("Salão de Jogos", RoomKind.Counter, ObstacleType.Person, 1700, 15000, 5f),
                Room("Sala de Câmeras", RoomKind.CameraRoom, ObstacleType.System, 1800, 17000, 5.5f),
                Room("Escritório do Bicheiro", RoomKind.Office, ObstacleType.Person, 1900, 25000, 5.5f),
                Room("Cofre", RoomKind.Vault, ObstacleType.Lock, 2100, 30000, 6f));

            var banco = Heist("banco-central", "Banco Central de Nova Guanabara", 5, cassino, 300f,
                "O golpe do século. Dona Cida passou trinta anos planejando: um túnel saindo de uma loja de material de construção até o cofre do Banco Central.",
                Room("Túnel", RoomKind.Tunnel, ObstacleType.Lock, 2300, 10000, 1f),
                Room("Túnel Fundo", RoomKind.Tunnel, ObstacleType.System, 2500, 20000, 1.5f),
                Room("Subsolo", RoomKind.Garage, ObstacleType.Person, 2700, 40000, 5f),
                Room("Corredor de Segurança", RoomKind.Corridor, ObstacleType.System, 2900, 60000, 6f),
                Room("Sala de Câmeras", RoomKind.CameraRoom, ObstacleType.System, 3100, 90000, 6f),
                Room("Antecâmara", RoomKind.Office, ObstacleType.Lock, 3300, 130000, 7f),
                Room("Cofre Central", RoomKind.Vault, ObstacleType.Lock, 3800, 250000, 8f));

            var heists = new List<HeistDef> { loterica, joalheria, cambio, cassino, banco };
            for (int i = 0; i < heists.Count; i++)
            {
                heists[i].mapPosition = new Vector2(-6f + i * 3f, (i % 2 == 0) ? 0f : 1.5f);
                EditorUtility.SetDirty(heists[i]);
            }
            return heists;
        }

        static List<HeistEventDef> CreateEvents()
        {
            var tutorial = MakeEvent("tutorial-seu-ze", "Seu Zé desconfiou",
                "O Seu Zé levantou da cadeira e olhou pro bando: \"Vocês vão apostar ou não?\"", e => e.tutorialOnly = true,
                Choice("Fazer uma fezinha na Mega", true, ThiefType.ConArtist, CheckStat.None, 1f, 0f,
                    Fx("O Seu Zé ficou feliz com a aposta e voltou pro rádio.", alert: -5f), Fx("")),
                Choice("Fingir que tá conferindo o resultado", false, ThiefType.ConArtist, CheckStat.Nerves, 0.6f, 0.004f,
                    Fx("Ele engoliu a história."), Fx("Ele ficou de olho em vocês.", alert: 15f)));

            var guarda = MakeEvent("guarda-cafe", "Guarda voltou do café",
                "O guarda voltou mais cedo do café e está parado bem na porta.", e => { },
                Choice("Puxar papo sobre o Flamengo", true, ThiefType.ConArtist, CheckStat.Nerves, 0.5f, 0.006f,
                    Fx("O guarda ficou dez minutos falando do Zico.", alert: -10f), Fx("Ele é vascaíno. Climão.", alert: 10f)),
                Choice("Esconder no banheiro", false, ThiefType.ConArtist, CheckStat.None, 1f, 0f,
                    Fx("Vocês perderam um tempinho, mas ninguém viu.", delay: 8f), Fx("")),
                Choice("Sair correndo", false, ThiefType.ConArtist, CheckStat.None, 1f, 0f,
                    Fx("Passaram, mas o barulho foi grande.", alert: 20f), Fx("")));

            var fechadura = MakeEvent("fechadura-emperrada", "Fechadura emperrada",
                "A fechadura travou no meio do serviço.", e => { e.anyRoom = false; e.roomObstacle = ObstacleType.Lock; },
                Choice("Usar a gazua de estimação", true, ThiefType.Locksmith, CheckStat.Skill, 0.5f, 0.005f,
                    Fx("Abriu macio.", alert: -5f), Fx("Demorou, mas abriu.", delay: 6f)),
                Choice("Na marretada", false, ThiefType.Locksmith, CheckStat.None, 1f, 0f,
                    Fx("Abriu. E o prédio inteiro ouviu.", alert: 25f), Fx("")));

            var luz = MakeEvent("queda-de-luz", "Caiu a luz do quarteirão",
                "Tudo apagou de uma vez. O gerador de emergência vai ligar daqui a pouco.", e => e.minTier = 2,
                Choice("Religar o sistema no modo silencioso", true, ThiefType.Hacker, CheckStat.Skill, 0.6f, 0.004f,
                    Fx("As câmeras voltaram… gravando o nada.", alert: -15f), Fx("O alarme reiniciou apitando.", alert: 10f)),
                Choice("Aproveitar o escuro", false, ThiefType.Hacker, CheckStat.Stealth, 0.4f, 0.005f,
                    Fx("No escuro sobrou tempo pra encher a sacola.", bag: 10f), Fx("Alguém tropeçou num balde.", alert: 15f)));

            return new List<HeistEventDef> { tutorial, guarda, fechadura, luz };
        }

        static List<UpgradeDef> CreateUpgrades() => new List<UpgradeDef>
        {
            Upgrade("van", "Van", "+1 vaga no golpe.", UpgradeEffect.HeistSlots, 2, 300, 4f, 1f),
            Upgrade("quartos", "Quartos", "+1 vaga na crew.", UpgradeEffect.CrewSlots, 5, 200, 2.2f, 1f),
            Upgrade("ferramentas", "Ferramentas", "+5% de Perícia pra todo mundo.", UpgradeEffect.SkillPercent, 10, 150, 1.6f, 5f),
            Upgrade("tenis-de-molinha", "Tênis de molinha", "+5% de Discrição pra todo mundo.", UpgradeEffect.StealthPercent, 10, 150, 1.6f, 5f),
            Upgrade("advogado", "Advogado de porta de cadeia", "-10% no tempo de prisão e na fiança.", UpgradeEffect.LawyerPercent, 5, 250, 1.8f, 10f),
            Upgrade("contatos", "Contatos", "Candidatos de nível mais alto.", UpgradeEffect.ContactsLevel, 5, 400, 2f, 1f),
            Upgrade("lavanderia", "Lavanderia", "+5% em todo loot.", UpgradeEffect.LaundryPercent, 10, 500, 1.7f, 5f),
            Upgrade("radio-da-policia", "Rádio da polícia", "+2 s na contagem da polícia.", UpgradeEffect.PoliceRadioSeconds, 5, 350, 1.8f, 2f),
            Upgrade("ponto-de-bico", "Ponto de bico", "+10% de renda dos bicos.", UpgradeEffect.SideJobPercent, 10, 100, 1.6f, 10f),
        };

        static List<HeistModifierDef> CreateModifiers() => new List<HeistModifierDef>
        {
            Modifier("feriado", "Feriado", "Pouca gente: Pessoa -30% de dificuldade.", m => m.personDifficultyMult = 0.7f),
            Modifier("troca-de-turno", "Troca de turno", "-20% de alerta nas 2 primeiras salas.", m => { m.firstRoomsAlertMult = 0.8f; m.firstRoomsCount = 2; }),
            Modifier("cofre-novo", "Cofre novo", "Fechadura +40% de dificuldade, +30% de loot.", m => { m.lockDifficultyMult = 1.4f; m.lootMult = 1.3f; }),
            Modifier("blitz", "Blitz na área", "Polícia chega 3 s mais rápido.", m => m.policeCountdownDelta = -3f),
            Modifier("dia-de-pagamento", "Dia de pagamento", "+50% de loot, +20% de alerta.", m => { m.lootMult = 1.5f; m.alertMult = 1.2f; }),
            Modifier("cameras-quebradas", "Câmeras quebradas", "Sistema -50% de dificuldade.", m => m.systemDifficultyMult = 0.5f),
        };

        static List<FamePerkDef> CreateFamePerks() => new List<FamePerkDef>
        {
            Perk("reputacao", "Reputação", "-10% no preço dos recrutas.", FamePerkEffect.RecruitDiscountPercent, 5, 1, 1.6f, 10f),
            Perk("heranca", "Herança", "Começa cada ciclo com Cr$ 1.000 a mais.", FamePerkEffect.StartingMoney, 5, 1, 1.8f, 1000f),
            Perk("lenda", "Lenda", "Recrutas nascem com +1 nível.", FamePerkEffect.RecruitBonusLevels, 3, 2, 2f, 1f),
            Perk("esquema", "Esquema", "+10% em todo loot.", FamePerkEffect.LootPercent, 10, 1, 1.5f, 10f),
            Perk("rede", "Rede", "+15% de renda dos bicos.", FamePerkEffect.SideJobPercent, 10, 1, 1.5f, 15f),
            Perk("sangue-frio-da-firma", "Sangue-frio", "-5% de alerta em todo golpe.", FamePerkEffect.AlertReductionPercent, 5, 2, 1.7f, 5f),
        };

        // ------------------------------------------------------------------ helpers de dados

        static string ThiefTypeAsset(string folder) => $"{Root}/Thieves/{folder}/ThiefType_{folder}.asset";

        static ThiefTypeDef MakeThiefType(string path, string id, string name, ThiefType type, string description,
            bool hasSpecialty, ObstacleType specialty, float skill, float stealth, float nerves, string[] barks, Color color) =>
            Create<ThiefTypeDef>(path, t =>
            {
                Def(t, id, name, description);
                t.type = type; t.hasSpecialty = hasSpecialty; t.specialty = specialty;
                t.baseSkill = skill; t.baseStealth = stealth; t.baseNerves = nerves;
                t.barks = barks; t.color = color;
            });

        static SimpleAbilityDef Ability(string folder, string id, string name, string description, Action<SimpleAbilityDef> init) =>
            Create<SimpleAbilityDef>($"{Root}/Thieves/{folder}/Ability_{folder}.asset", a => { Def(a, id, name, description); init(a); });

        static SideJobDef SideJob(string folder, string id, string name, string description, ThiefType type) =>
            Create<SideJobDef>($"{Root}/Thieves/{folder}/SideJob_{folder}.asset", j =>
            {
                Def(j, id, name, description);
                j.hasPreferredType = true; j.preferredType = type; j.incomePerSecond = 3;
            });

        static TraitDef Trait(string id, string name, string description, Action<StatModifiers> init) =>
            Create<TraitDef>($"{Data}/Crew/Traits/Trait_{id}.asset", t => { Def(t, id, name, description); init(t.modifiers); });

        static RoomData Room(string name, RoomKind kind, ObstacleType obstacle, float difficulty, double loot, float alert) =>
            new RoomData { name = name, kind = kind, obstacle = obstacle, difficulty = difficulty, loot = loot, alertPerSecond = alert };

        static HeistDef Heist(string id, string name, int tier, HeistDef unlockedBy, float jail, string briefing, params RoomData[] rooms) =>
            Create<HeistDef>($"{Data}/Heists/Heist_{tier}_{id}.asset", h =>
            {
                Def(h, id, name, briefing);
                h.tier = tier; h.unlockedBy = unlockedBy; h.jailSeconds = jail; h.briefing = briefing;
                h.rooms = new List<RoomData>(rooms);
            });

        static EventEffect Fx(string text, float alert = 0f, float bag = 0f, float delay = 0f, bool jail = false) =>
            new EventEffect { resultText = text, alertDelta = alert, bagPercentDelta = bag, delaySeconds = delay, jailRandomThief = jail };

        static EventChoice Choice(string label, bool requiresType, ThiefType type, CheckStat check, float baseChance, float perPoint,
            EventEffect success, EventEffect failure) =>
            new EventChoice
            {
                label = label, requiresType = requiresType, requiredType = type, check = check,
                baseChance = baseChance, chancePerStatPoint = perPoint, success = success, failure = failure
            };

        static HeistEventDef MakeEvent(string id, string title, string body, Action<HeistEventDef> init, params EventChoice[] choices) =>
            Create<HeistEventDef>($"{Data}/Events/Event_{id}.asset", e =>
            {
                Def(e, id, title, body);
                e.body = body;
                init(e);
                e.choices = new List<EventChoice>(choices);
            });

        static UpgradeDef Upgrade(string id, string name, string description, UpgradeEffect effect, int max, double cost, float growth, float value) =>
            Create<UpgradeDef>($"{Data}/Economy/Upgrades/Upgrade_{id}.asset", u =>
            {
                Def(u, id, name, description);
                u.effect = effect; u.maxLevel = max; u.baseCost = cost; u.costGrowth = growth; u.valuePerLevel = value;
            });

        static HeistModifierDef Modifier(string id, string name, string description, Action<HeistModifierDef> init) =>
            Create<HeistModifierDef>($"{Data}/Progression/Modifiers/Modifier_{id}.asset", m => { Def(m, id, name, description); init(m); });

        static FamePerkDef Perk(string id, string name, string description, FamePerkEffect effect, int max, int cost, float growth, float value) =>
            Create<FamePerkDef>($"{Data}/Progression/FamePerks/FamePerk_{id}.asset", p =>
            {
                Def(p, id, name, description);
                p.effect = effect; p.maxLevel = max; p.baseCost = cost; p.costGrowth = growth; p.valuePerLevel = value;
            });

        static void Def(DefBase def, string id, string name, string description)
        {
            def.id = id; def.displayName = name; def.description = description;
        }

        static T Create<T>(string path, Action<T> init) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ------------------------------------------------------------------ cenas

        static void CreateScenes(GameDatabase db)
        {
            var scenes = Root + "/Scenes";
            MakeScene(scenes + "/Boot.unity", () => new GameObject("Boot").AddComponent<BootLoader>());
            MakeScene(scenes + "/MainMenu.unity", () => Placeholder(ScenePlaceholder.Mode.MainMenu, "Menu Principal", "Enzo"));
            MakeScene(scenes + "/Hideout.unity", () => Placeholder(ScenePlaceholder.Mode.Hideout, "Esconderijo", "Enzo"));
            MakeScene(scenes + "/Heist.unity", () =>
            {
                new GameObject("HeistSceneBootstrap").AddComponent<HeistSceneBootstrap>();
                new GameObject("--- CENA DO GOLPE (Miguel) ---");
                new GameObject("--- HUD (prefab do Enzo) ---");
                Placeholder(ScenePlaceholder.Mode.Heist, "Golpe", "Miguel (cena) + Enzo (HUD)");
            });

            foreach (var person in new[] { "Leonardo", "Nicolas", "Enzo", "Miguel" })
                MakeScene($"{Root}/Sandbox/{person}/Sandbox_{person}.unity",
                    () => Placeholder(ScenePlaceholder.Mode.Sandbox, "Sandbox do " + person, person));

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scenes + "/Boot.unity", true),
                new EditorBuildSettingsScene(scenes + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(scenes + "/Hideout.unity", true),
                new EditorBuildSettingsScene(scenes + "/Heist.unity", true),
            };
        }

        static void Placeholder(ScenePlaceholder.Mode mode, string title, string owner)
        {
            var placeholder = new GameObject("Placeholder (apagar quando a tela real existir)").AddComponent<ScenePlaceholder>();
            placeholder.mode = mode; placeholder.title = title; placeholder.owner = owner;
        }

        static void MakeScene(string path, Action populate)
        {
            if (File.Exists(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            if (!AssetDatabase.CopyAsset(SceneTemplate, path))
            {
                var empty = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(empty, path);
            }
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            populate();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ------------------------------------------------------------------ player / build

        static void ConfigurePlayer()
        {
            PlayerSettings.productName = "A Firma";
            PlayerSettings.companyName = "A Firma";
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
        }
    }
}
