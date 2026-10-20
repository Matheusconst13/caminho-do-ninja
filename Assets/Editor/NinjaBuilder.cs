using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Monta as cenas, prefabs e UI do "Caminho do Ninja" e gera o build WebGL.
// Menu: Ninja > Montar cenas / Ninja > Build WebGL
// (ou -executeMethod NinjaBuilder.Build / NinjaBuilder.BuildWebGL)
public static class NinjaBuilder
{
    const string MenuScene = "Assets/Scenes/MenuInicial.unity";
    const string GameScene = "Assets/Scenes/Jogo.unity";
    const string SceneTemplate = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
    const string Sprites = "Assets/Sprites/Ninja/";
    const string Audio = "Assets/Audio/";
    const string Prefabs = "Assets/Prefabs/";

    static readonly Color CorBotao = new Color(0.55f, 0.1f, 0.1f);
    static readonly Color CorBotaoAtivo = new Color(0.86f, 0.17f, 0.17f);
    static readonly Color CorPainel = new Color(0.07f, 0.06f, 0.12f, 0.94f);

    static Font font;
    static Sprite uiSprite;
    static Sprite quadrado;
    static PhysicsMaterial2D semAtrito;

    [MenuItem("Ninja/Montar cenas")]
    public static void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        foreach (var tag in new[] { "Coletavel", "Ampulheta", "Inimigo", "Armadilha" })
            AddTag(tag);
        ImportarSprites();
        AssetDatabase.ImportAsset("Assets/Audio", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        quadrado = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Square.png");

        semAtrito = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/SemAtrito.physicsMaterial2D");
        if (semAtrito == null)
        {
            semAtrito = new PhysicsMaterial2D("SemAtrito") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(semAtrito, "Assets/Settings/SemAtrito.physicsMaterial2D");
        }

        foreach (var cena in new[] { GameScene, MenuScene })
        {
            AssetDatabase.DeleteAsset(cena);
            AssetDatabase.CopyAsset(SceneTemplate, cena);
        }

        BuildGameScene();
        BuildMenuScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScene, true),
            new EditorBuildSettingsScene(GameScene, true),
        };
        PlayerSettings.productName = "Caminho do Ninja";
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MenuScene);
        Debug.Log("NinjaBuilder: cenas montadas com sucesso.");
    }

    // Monta as cenas e gera o build WebGL de uma vez (usado na linha de comando)
    public static void BuildAll()
    {
        Build();
        BuildWebGL();
    }

    [MenuItem("Ninja/Build WebGL")]
    public static void BuildWebGL()
    {
        PlayerSettings.productName = "Caminho do Ninja";
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 600;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;   // itch.io não envia o header de gzip
        PlayerSettings.WebGL.template = "APPLICATION:Minimal"; // só o canvas: cabe certinho no embed do itch.io

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { MenuScene, GameScene },
            locationPathName = "Build/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });
        if (report.summary.result == BuildResult.Succeeded)
        {
            // O canvas "inline" do template gera uma barra de rolagem de poucos pixels no embed
            string index = "Build/WebGL/index.html";
            string html = System.IO.File.ReadAllText(index);
            html = html.Replace("margin: 0;\">", "margin: 0; overflow: hidden;\">")
                       .Replace("style=\"width: 960px; height: 600px;", "style=\"display: block; margin: 0 auto; width: 960px; height: 600px;");
            System.IO.File.WriteAllText(index, html);
        }
        Debug.Log("NinjaBuilder: build WebGL " + report.summary.result + " (" + report.summary.totalSize / 1024 / 1024 + " MB)");
        if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    // ================================================================ cena do jogo

    static void BuildGameScene()
    {
        var scene = EditorSceneManager.OpenScene(GameScene);
        SetupCamera(new Color(0.08f, 0.07f, 0.12f));

        // ---------- arena (dojo): tatame, paredes de madeira e caixas ----------
        var arena = new GameObject("Arena").transform;
        var chao = NewSprite("Tatame", Spr("Tatame"), Color.white, new Vector3(0, -0.5f), -10, arena);
        Tiled(chao, new Vector2(15, 9));

        var paredes = new GameObject("Paredes").transform;
        paredes.SetParent(arena);
        Bloco("Parede Cima", "Madeira", new Vector2(0, 4.25f), new Vector2(16, 0.5f), paredes);
        Bloco("Parede Baixo", "Madeira", new Vector2(0, -5.25f), new Vector2(16, 0.5f), paredes);
        Bloco("Parede Esquerda", "Madeira", new Vector2(-7.75f, -0.5f), new Vector2(0.5f, 10), paredes);
        Bloco("Parede Direita", "Madeira", new Vector2(7.75f, -0.5f), new Vector2(0.5f, 10), paredes);

        var caixas = new GameObject("Caixas").transform;
        caixas.SetParent(arena);
        Bloco("Caixa A", "Caixa", new Vector2(-3.5f, 1.5f), new Vector2(1, 2), caixas);
        Bloco("Caixa B", "Caixa", new Vector2(3.5f, 1.5f), new Vector2(1, 2), caixas);
        Bloco("Caixa C", "Caixa", new Vector2(0, -1.5f), new Vector2(4, 1), caixas);
        Bloco("Caixa D", "Caixa", new Vector2(-5f, -2.5f), new Vector2(1, 1), caixas);
        Bloco("Caixa E", "Caixa", new Vector2(5f, -2.5f), new Vector2(1, 1), caixas);

        // ---------- prefabs ----------
        var playerPrefab = CriarPlayerPrefab();
        var shurikenPrefab = CriarItemPrefab("Shuriken", "Coletavel", 0.7f, 270f, 0f);
        var ampulhetaPrefab = CriarItemPrefab("Ampulheta", "Ampulheta", 0.75f, 0f, 0.12f);
        var estrepesPrefab = CriarEstrepesPrefab();
        var samuraiPrefab = CriarSamuraiPrefab();
        var sombraPrefab = CriarSombraPrefab();

        var player = Instanciar(playerPrefab, new Vector2(0, 1.5f), null);

        var itens = new GameObject("Shurikens").transform;
        foreach (var p in new[]
                 {
                     new Vector2(-6.5f, 3.2f), new Vector2(0, 3.2f), new Vector2(6.5f, 3.2f),
                     new Vector2(-6.5f, -4.3f), new Vector2(6.5f, -4.3f), new Vector2(0, -0.3f),
                     new Vector2(0, -3.4f), new Vector2(-5.5f, 0.5f), new Vector2(5.5f, 0.5f),
                     new Vector2(2.5f, -4.3f),
                 })
            Instanciar(shurikenPrefab, p, itens);

        var ampulhetas = new GameObject("Ampulhetas").transform;
        foreach (var p in new[] { new Vector2(-2f, 2.2f), new Vector2(2.8f, -3.4f), new Vector2(-6.5f, -1.5f) })
            Instanciar(ampulhetaPrefab, p, ampulhetas);

        var armadilhas = new GameObject("Armadilhas").transform;
        foreach (var p in new[] { new Vector2(-1.8f, 1f), new Vector2(1.8f, 1f), new Vector2(-2.5f, -4.3f), new Vector2(4.3f, -4.3f) })
            Instanciar(estrepesPrefab, p, armadilhas);

        var inimigos = new GameObject("Inimigos").transform;
        Samurai(samuraiPrefab, new Vector2(-5, 3.3f), new Vector2(5, 3.3f), 2.6f, inimigos);
        Samurai(samuraiPrefab, new Vector2(6.3f, -3.2f), new Vector2(6.3f, 2.2f), 2.2f, inimigos);
        Samurai(samuraiPrefab, new Vector2(-6.3f, 2.2f), new Vector2(-6.3f, -3.2f), 2.2f, inimigos);

        var sombra = Instanciar(sombraPrefab, new Vector2(-3, -3.5f), inimigos);
        var perseguidor = sombra.GetComponent<InimigoPerseguidor>();
        perseguidor.pontoA = new Vector2(-3, -3.5f);
        perseguidor.pontoB = new Vector2(3, -3.5f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(perseguidor);

        // ---------- GameManager + áudio ----------
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        gm.musica = NovaFonteDeAudio("Musica", gmGo.transform, Clip("MusicaJogo"), true, 0.6f);
        gm.sfx = NovaFonteDeAudio("Efeitos", gmGo.transform, null, false, 1f);
        gm.somDano = Clip("Dano");
        gm.somVitoria = Clip("Vitoria");
        gm.somDerrota = Clip("Derrota");
        gm.somTique = Clip("Tique");
        gm.somClique = Clip("Clique");

        // ---------- HUD ----------
        var canvas = NewCanvas();
        var hud = NewPanel("HUD", canvas, new Color(0, 0, 0, 0.6f));
        Place(hud, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 100), new Vector2(0.5f, 1));

        gm.coracoes = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var c = NewImage("Coracao " + (i + 1), hud, Spr("Coracao"), Color.white);
            Place(c.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30 + i * 70, 0), new Vector2(64, 64), new Vector2(0, 0.5f));
            gm.coracoes[i] = c;
        }
        gm.coracaoCheio = Spr("Coracao");
        gm.coracaoVazio = Spr("CoracaoVazio");

        var staminaLabel = NewText("StaminaLabel", hud, "STAMINA", 22, TextAnchor.LowerLeft);
        Place(staminaLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(260, 6), new Vector2(300, 30), new Vector2(0, 0));
        var staminaFundo = NewImage("StaminaFundo", hud, quadrado, new Color(0.15f, 0.15f, 0.2f));
        Place(staminaFundo.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(260, -2), new Vector2(300, 22), new Vector2(0, 1));
        var staminaBarra = NewImage("StaminaBarra", staminaFundo.transform, quadrado, new Color(0.3f, 0.79f, 0.94f));
        Stretch(staminaBarra.rectTransform);
        staminaBarra.type = Image.Type.Filled;
        staminaBarra.fillMethod = Image.FillMethod.Horizontal;
        staminaBarra.fillOrigin = (int)Image.OriginHorizontal.Left;
        gm.staminaBarra = staminaBarra;

        var tempoLabel = NewText("TempoLabel", hud, "TEMPO", 22, TextAnchor.UpperCenter);
        Place(tempoLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(300, 30), new Vector2(0.5f, 1));
        gm.tempoText = NewText("Tempo", hud, "01:30", 60, TextAnchor.UpperCenter);
        Place(gm.tempoText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(300, 70), new Vector2(0.5f, 1));

        gm.pontosText = NewText("Pontos", hud, "Pontos: 0", 34, TextAnchor.MiddleRight);
        Place(gm.pontosText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -10), new Vector2(500, 40), new Vector2(1, 1));
        gm.shurikensText = NewText("Shurikens", hud, "Shurikens: 0 / 10", 34, TextAnchor.MiddleRight);
        Place(gm.shurikensText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -52), new Vector2(500, 40), new Vector2(1, 1));

        gm.avisoText = NewText("Aviso", canvas, "", 44, TextAnchor.MiddleCenter);
        Place(gm.avisoText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(1600, 80));
        Contorno(gm.avisoText);

        // ---------- pausa ----------
        var pause = NewPanel("PausePanel", canvas, new Color(0, 0, 0, 0.6f));
        Stretch(pause);
        var pauseBox = NewPanel("Caixa", pause, CorPainel);
        Place(pauseBox, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(760, 640));
        var pauseTitulo = NewText("Titulo", pauseBox, "PAUSADO", 80, TextAnchor.MiddleCenter);
        Place(pauseTitulo.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 230), new Vector2(700, 110));
        var dica = NewText("Dica", pauseBox, "Mover: WASD / setas / analógico  |  Dash: Espaço / A", 26, TextAnchor.MiddleCenter);
        Place(dica.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 150), new Vector2(720, 40));
        dica.color = new Color(1, 1, 1, 0.7f);
        gm.primeiroBotaoPause = NewButton("BotaoContinuar", pauseBox, "Continuar", new Vector2(0, 50), gm.Continuar);
        NewButton("BotaoReiniciar", pauseBox, "Reiniciar", new Vector2(0, -75), gm.JogarNovamente);
        NewButton("BotaoMenu", pauseBox, "Menu", new Vector2(0, -200), gm.VoltarAoMenu);
        gm.pausePanel = pause.gameObject;

        // ---------- fim de jogo ----------
        var fim = NewPanel("FimPanel", canvas, new Color(0, 0, 0, 0.65f));
        Stretch(fim);
        var fimBox = NewPanel("Caixa", fim, CorPainel);
        Place(fimBox, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(960, 780));
        gm.fimTitulo = NewText("Titulo", fimBox, "Missão cumprida!", 84, TextAnchor.MiddleCenter);
        Place(gm.fimTitulo.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 290), new Vector2(900, 110));
        Contorno(gm.fimTitulo);
        gm.fimMotivo = NewText("Motivo", fimBox, "", 38, TextAnchor.MiddleCenter);
        Place(gm.fimMotivo.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 200), new Vector2(900, 50));
        gm.fimStats = NewText("Stats", fimBox, "", 38, TextAnchor.MiddleCenter);
        gm.fimStats.lineSpacing = 1.2f;
        Place(gm.fimStats.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 40), new Vector2(900, 240));
        gm.primeiroBotaoFim = NewButton("BotaoJogarNovamente", fimBox, "Jogar novamente", new Vector2(0, -190), gm.JogarNovamente);
        NewButton("BotaoMenu", fimBox, "Menu", new Vector2(0, -310), gm.VoltarAoMenu);
        gm.fimPanel = fim.gameObject;

        NewEventSystem();
        EditorSceneManager.SaveScene(scene);
    }

    // ================================================================ menu inicial

    static void BuildMenuScene()
    {
        var scene = EditorSceneManager.OpenScene(MenuScene);
        SetupCamera(new Color(0.06f, 0.05f, 0.15f));

        var canvas = NewCanvas();
        var fundo = NewImage("Fundo", canvas, Spr("FundoMenu"), Color.white);
        Stretch(fundo.rectTransform);

        var menuGo = new GameObject("MenuController");
        var menu = menuGo.AddComponent<MenuInicial>();
        NovaFonteDeAudio("Musica", menuGo.transform, Clip("MusicaMenu"), true, 0.6f);
        menu.sfx = NovaFonteDeAudio("Efeitos", menuGo.transform, null, false, 1f);
        menu.somClique = Clip("Clique");

        var principal = new GameObject("PainelPrincipal", typeof(RectTransform)).GetComponent<RectTransform>();
        principal.SetParent(canvas, false);
        Stretch(principal);

        var titulo1 = NewText("Titulo", principal, "CAMINHO DO", 96, TextAnchor.MiddleCenter);
        Place(titulo1.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 380), new Vector2(1600, 120));
        titulo1.color = new Color(1f, 0.94f, 0.9f);
        Contorno(titulo1);
        var titulo2 = NewText("Titulo2", principal, "NINJA", 150, TextAnchor.MiddleCenter);
        Place(titulo2.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 250), new Vector2(1600, 170));
        titulo2.color = new Color(0.86f, 0.17f, 0.17f);
        Contorno(titulo2);
        var sub = NewText("Subtitulo", principal, "Recolha as shurikens do dojo antes que o tempo acabe", 38, TextAnchor.MiddleCenter);
        Place(sub.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 140), new Vector2(1600, 60));
        Contorno(sub);

        var ninja = NewImage("Ninja", principal, Spr("Ninja_0"), Color.white);
        Place(ninja.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(-560, -180), new Vector2(320, 320));
        var samurai = NewImage("Samurai", principal, Spr("Samurai_0"), Color.white);
        Place(samurai.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(560, -180), new Vector2(320, 320));

        menu.botaoJogar = NewButton("BotaoJogar", principal, "Jogar", new Vector2(0, 0), menu.Jogar);
        NewButton("BotaoComoJogar", principal, "Como jogar", new Vector2(0, -125), menu.ComoJogar);
        menu.botaoSair = NewButton("BotaoSair", principal, "Sair", new Vector2(0, -250), menu.Sair);
        menu.painelPrincipal = principal.gameObject;

        var painel = NewPanel("PainelComoJogar", canvas, CorPainel);
        Place(painel, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(1400, 860));
        var comoTitulo = NewText("Titulo", painel, "COMO JOGAR", 72, TextAnchor.MiddleCenter);
        Place(comoTitulo.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 340), new Vector2(1300, 100));
        comoTitulo.color = new Color(0.86f, 0.17f, 0.17f);
        var texto = NewText("Texto", painel,
            "Recolha as 10 shurikens antes que o cronômetro chegue a zero.\n" +
            "Ampulhetas dão +10 segundos. Sobra de tempo vira bônus de pontos.\n\n" +
            "Mover:  WASD / setas | analógico esquerdo / direcional\n" +
            "Dash:  Espaço ou Shift | botão A / RT  (gasta stamina e deixa invulnerável)\n" +
            "Pausar:  Esc ou P | botão Start\n\n" +
            "Cuidado: samurais patrulham o dojo, o ninja sombra persegue\n" +
            "quem entra na área roxa e as estrepes (makibishi) machucam.\n" +
            "Você tem 3 vidas.", 34, TextAnchor.MiddleCenter);
        texto.lineSpacing = 1.15f;
        Place(texto.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 20), new Vector2(1320, 520));
        menu.botaoVoltar = NewButton("BotaoVoltar", painel, "Voltar", new Vector2(0, -340), menu.Voltar);
        menu.painelComoJogar = painel.gameObject;

        NewEventSystem();
        EditorSceneManager.SaveScene(scene);
    }

    // ================================================================ prefabs

    static GameObject CriarPlayerPrefab()
    {
        var go = NewSprite("Player", Spr("Ninja_0"), Color.white, Vector3.zero, 3, null);
        go.tag = "Player";
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.38f;
        col.sharedMaterial = semAtrito;
        var audio = go.AddComponent<AudioSource>();
        audio.clip = Clip("ShurikenColetar");
        audio.playOnAwake = false;
        audio.loop = false;
        var mov = go.AddComponent<PlayerMovement>();
        mov.speed = 5f;
        mov.somDash = Clip("Dash");
        mov.somAmpulheta = Clip("Ampulheta");
        go.AddComponent<PlayerHealth>();
        Animar(go, "Ninja", 8f, true);
        return SalvarPrefab(go);
    }

    static GameObject CriarItemPrefab(string sprite, string tag, float escala, float giro, float flutuar)
    {
        var go = NewSprite(sprite, Spr(sprite), Color.white, Vector3.zero, 1, null);
        go.transform.localScale = Vector3.one * escala;
        go.tag = tag;
        go.AddComponent<CircleCollider2D>().isTrigger = true;
        var girar = go.AddComponent<Girar>();
        girar.grausPorSegundo = giro;
        girar.flutuar = flutuar;
        return SalvarPrefab(go);
    }

    static GameObject CriarEstrepesPrefab()
    {
        var go = NewSprite("Estrepes", Spr("Estrepes"), Color.white, Vector3.zero, 0, null);
        go.tag = "Armadilha";
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.8f);
        // Mancha de perigo: sprite básico (círculo) com a cor trocada
        var perigo = NewSprite("Perigo", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Circle.png"),
            new Color(0.85f, 0.1f, 0.1f, 0.35f), Vector3.zero, -1, go.transform);
        perigo.transform.localScale = Vector3.one * 1.1f;
        return SalvarPrefab(go);
    }

    static GameObject CriarSamuraiPrefab()
    {
        var go = NewSprite("Samurai", Spr("Samurai_0"), Color.white, Vector3.zero, 2, null);
        go.tag = "Inimigo";
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        go.AddComponent<CircleCollider2D>().radius = 0.42f;
        go.AddComponent<InimigoPatrulha>();
        Animar(go, "Samurai", 6f, false);
        return SalvarPrefab(go);
    }

    static GameObject CriarSombraPrefab()
    {
        var go = NewSprite("NinjaSombra", Spr("Sombra_0"), Color.white, Vector3.zero, 2, null);
        go.tag = "Inimigo";
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.mass = 50f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.sharedMaterial = semAtrito;
        var ia = go.AddComponent<InimigoPerseguidor>();
        ia.somAlerta = Clip("Alerta");

        // Área de detecção: sprite básico (círculo) com a cor trocada
        var area = NewSprite("AreaDeteccao", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Circle.png"),
            new Color(0.6f, 0.3f, 1f, 0.12f), Vector3.zero, -5, go.transform);
        area.transform.localScale = Vector3.one * ia.raioDeteccao * 2f;
        ia.areaDeteccao = area.GetComponent<SpriteRenderer>();

        Animar(go, "Sombra", 8f, true);
        return SalvarPrefab(go);
    }

    static void Samurai(GameObject prefab, Vector2 a, Vector2 b, float velocidade, Transform pai)
    {
        var go = Instanciar(prefab, a, pai);
        var patrulha = go.GetComponent<InimigoPatrulha>();
        patrulha.pontoA = a;
        patrulha.pontoB = b;
        patrulha.velocidade = velocidade;
        PrefabUtility.RecordPrefabInstancePropertyModifications(patrulha);
    }

    static void Animar(GameObject go, string nome, float fps, bool soQuandoMove)
    {
        var anim = go.AddComponent<AnimacaoSprite>();
        anim.quadros = new[] { Spr(nome + "_0"), Spr(nome + "_1") };
        anim.fps = fps;
        anim.soQuandoMove = soQuandoMove;
    }

    static GameObject SalvarPrefab(GameObject go)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, Prefabs + go.name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject Instanciar(GameObject prefab, Vector2 pos, Transform pai)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
        go.transform.position = pos;
        return go;
    }

    // ================================================================ helpers

    static void ImportarSprites()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites/Ninja" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            bool lado = path.Contains("Tatame") || path.Contains("Madeira") || path.Contains("Caixa");
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = path.Contains("Tatame") ? 32 : 16;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = lado ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = lado ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }
    }

    static Sprite Spr(string nome)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + nome + ".png");
        if (s == null) throw new System.Exception("Sprite não encontrado: " + nome);
        return s;
    }

    static AudioClip Clip(string nome)
    {
        var c = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + nome + ".wav");
        if (c == null) throw new System.Exception("Áudio não encontrado: " + nome);
        return c;
    }

    static void AddTag(string tag)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetupCamera(Color background)
    {
        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 5.5f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
    }

    static GameObject NewSprite(string name, Sprite sprite, Color color, Vector3 pos, int ordem, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = ordem;
        return go;
    }

    static void Tiled(GameObject go, Vector2 tamanho)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = tamanho;
    }

    static void Bloco(string name, string sprite, Vector2 pos, Vector2 tamanho, Transform parent)
    {
        var go = NewSprite(name, Spr(sprite), Color.white, pos, 0, parent);
        go.transform.position = pos;
        Tiled(go, tamanho);
        go.AddComponent<BoxCollider2D>().size = tamanho;
    }

    static AudioSource NovaFonteDeAudio(string name, Transform parent, AudioClip clip, bool musica, float volume)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var a = go.AddComponent<AudioSource>();
        a.clip = clip;
        a.loop = musica;
        a.playOnAwake = musica;
        a.volume = volume;
        return a;
    }

    static Transform NewCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return go.transform;
    }

    static void NewEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    static RectTransform NewPanel(string name, Transform parent, Color cor)
    {
        var img = NewImage(name, parent, uiSprite, cor);
        img.type = Image.Type.Sliced;
        return img.rectTransform;
    }

    static Image NewImage(string name, Transform parent, Sprite sprite, Color cor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = cor;
        return img;
    }

    static Text NewText(string name, Transform parent, string content, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = anchor;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void Contorno(Text t)
    {
        var o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0, 0, 0, 0.85f);
        o.effectDistance = new Vector2(3, -3);
    }

    static GameObject NewButton(string name, Transform parent, string label, Vector2 pos, UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = uiSprite;
        img.type = Image.Type.Sliced;
        Place((RectTransform)go.transform, Vector2.one * 0.5f, Vector2.one * 0.5f, pos, new Vector2(520, 100));

        var btn = go.GetComponent<Button>();
        var cores = btn.colors;
        cores.normalColor = CorBotao;
        cores.highlightedColor = CorBotaoAtivo;
        cores.selectedColor = CorBotaoAtivo;
        cores.pressedColor = new Color(0.35f, 0.05f, 0.05f);
        cores.colorMultiplier = 1f;
        btn.colors = cores;

        var text = NewText("Texto", go.transform, label, 46, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        UnityEventTools.AddPersistentListener(btn.onClick, action);
        return go;
    }

    static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
