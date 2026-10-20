using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class NinjaTest
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    Component gm;
    Rigidbody2D player;
    GameObject fimPanel;

    IEnumerator CarregarJogo()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Jogo");
        yield return null;
        yield return null;
        gm = GameObject.Find("GameManager").GetComponent("GameManager");
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
        fimPanel = GameObject.Find("Canvas").transform.Find("FimPanel").gameObject;
    }

    T Campo<T>(string nome) => (T)gm.GetType().GetField(nome, F).GetValue(gm);
    void SetCampo(string nome, object v) => gm.GetType().GetField(nome, F).SetValue(gm, v);
    string Texto(string campo) => Campo<Text>(campo).text;

    [UnityTest]
    public IEnumerator MenuAbreJogo()
    {
        SceneManager.LoadScene("MenuInicial");
        yield return null;
        yield return null;
        var painel = GameObject.Find("Canvas").transform.Find("PainelComoJogar").gameObject;
        Assert.IsFalse(painel.activeSelf);
        GameObject.Find("BotaoComoJogar").GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(painel.activeSelf);
        GameObject.Find("BotaoVoltar").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(painel.activeSelf);
        GameObject.Find("BotaoJogar").GetComponent<Button>().onClick.Invoke();
        yield return null;
        yield return null;
        Assert.AreEqual("Jogo", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator ColetarTodasVence()
    {
        yield return CarregarJogo();
        Assert.AreEqual("Shurikens: 0 / 10", Texto("shurikensText"));
        Assert.AreEqual("01:30", Texto("tempoText"));
        var shurikens = GameObject.FindGameObjectsWithTag("Coletavel");
        Assert.AreEqual(10, shurikens.Length);
        foreach (var s in shurikens)
        {
            player.position = s.transform.position;
            for (int i = 0; i < 3 && Time.timeScale > 0f; i++) yield return new WaitForFixedUpdate();
        }
        yield return null;
        Assert.IsTrue(fimPanel.activeSelf, "painel de fim deveria aparecer");
        Assert.AreEqual("Missão cumprida!", Texto("fimTitulo"));
        StringAssert.Contains("Tempo da missão: 00:0", Texto("fimStats"));
        StringAssert.Contains("Bônus de tempo: +", Texto("fimStats"));
        Assert.AreEqual(0f, Time.timeScale);
        Debug.Log("FIM VITORIA:\n" + Texto("fimStats"));
    }

    [UnityTest]
    public IEnumerator EstrepesTresVezesDerrota()
    {
        yield return CarregarJogo();
        var trap = GameObject.FindGameObjectsWithTag("Armadilha")[0].transform.position;
        float limite = Time.time + 8f;
        while (!fimPanel.activeSelf && Time.time < limite)
        {
            player.position = trap;
            yield return new WaitForFixedUpdate();
        }
        Assert.IsTrue(fimPanel.activeSelf);
        Assert.AreEqual("Você foi derrotado!", Texto("fimMotivo"));
        Assert.AreEqual("Fim de jogo", Texto("fimTitulo"));
    }

    [UnityTest]
    public IEnumerator TempoAcabaDerrota()
    {
        yield return CarregarJogo();
        SetCampo("tempoRestante", 0.5f);
        yield return new WaitForSecondsRealtime(1f);
        Assert.IsTrue(fimPanel.activeSelf);
        Assert.AreEqual("O tempo acabou!", Texto("fimMotivo"));
        Assert.AreEqual("00:00", Texto("tempoText"));
    }

    [UnityTest]
    public IEnumerator AmpulhetaDaTempo()
    {
        yield return CarregarJogo();
        float antes = Campo<float>("tempoRestante");
        var amp = GameObject.FindGameObjectsWithTag("Ampulheta");
        Assert.AreEqual(3, amp.Length);
        player.position = amp[0].transform.position;
        for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
        float depois = Campo<float>("tempoRestante");
        Assert.Greater(depois, antes + 9f);
        Assert.AreEqual(2, GameObject.FindGameObjectsWithTag("Ampulheta").Length);
    }

    [UnityTest]
    public IEnumerator PausaEContinua()
    {
        yield return CarregarJogo();
        gm.GetType().GetMethod("Pausar").Invoke(gm, null);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(GameObject.Find("Canvas").transform.Find("PausePanel").gameObject.activeSelf);
        float t = Campo<float>("tempoRestante");
        yield return null;
        yield return null;
        Assert.AreEqual(t, Campo<float>("tempoRestante"));
        gm.GetType().GetMethod("Continuar").Invoke(gm, null);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator InimigosSeMovemEPerseguem()
    {
        yield return CarregarJogo();
        var sombra = GameObject.Find("NinjaSombra").GetComponent<Rigidbody2D>();
        var samurai = GameObject.Find("Samurai").transform;
        Vector3 s0 = samurai.position;
        // Coloca o jogador perto do ninja sombra e prende ele lá
        Vector2 alvo = sombra.position + new Vector2(3f, 0.6f);
        float d0 = Vector2.Distance(alvo, sombra.position);
        for (int i = 0; i < 25; i++)
        {
            player.position = alvo;
            player.linearVelocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }
        Assert.Less(Vector2.Distance(alvo, sombra.position), d0 - 0.8f, "ninja sombra deveria perseguir");
        Assert.Greater(Vector3.Distance(s0, samurai.position), 0.5f, "samurai deveria patrulhar");
    }

    [UnityTest]
    public IEnumerator Screenshot()
    {
        yield return CarregarJogo();
        for (int i = 0; i < 30; i++) yield return null;
        var cam = Camera.main;
        var canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 5f;
        var rt = new RenderTexture(1920, 1200, 24);
        cam.targetTexture = rt;
        yield return null;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1200, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1200), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(Application.dataPath + "/../Docs/screenshot_jogo.png", tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
    }
}
