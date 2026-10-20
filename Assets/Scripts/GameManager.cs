using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// GameManager estático (passo 17): pontos, cronômetro, vida, pausa e fim de jogo.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Usado por jogador e inimigos para saber se a partida está rolando
    public static bool Jogando => Instance != null && Instance.estado == Estado.Jogando;

    enum Estado { Jogando, Pausado, Fim }

    [Header("Regras")]
    public float tempoInicial = 90f;
    public float bonusAmpulheta = 10f;
    public float alertaTempo = 10f;
    public int pontosShuriken = 100;
    public int pontosPorSegundoRestante = 10;

    [Header("HUD")]
    public Text tempoText;
    public Text pontosText;
    public Text shurikensText;
    public Text avisoText;
    public Image[] coracoes;
    public Sprite coracaoCheio;
    public Sprite coracaoVazio;
    public Image staminaBarra;

    [Header("Telas")]
    public GameObject pausePanel;
    public GameObject fimPanel;
    public Text fimTitulo;
    public Text fimMotivo;
    public Text fimStats;
    public GameObject primeiroBotaoPause;
    public GameObject primeiroBotaoFim;

    [Header("Áudio")]
    public AudioSource musica;
    public AudioSource sfx;
    public AudioClip somDano;
    public AudioClip somVitoria;
    public AudioClip somDerrota;
    public AudioClip somTique;
    public AudioClip somClique;

    private Estado estado;
    private float tempoRestante;
    private float tempoDecorrido;
    private float avisoTimer;
    private int pontos;
    private int total;
    private int coletados;
    private int ultimoTique;
    private InputAction pausarAction;

    void Awake()
    {
        Instance = this;
        pausarAction = new InputAction("Pausar", InputActionType.Button);
        pausarAction.AddBinding("<Keyboard>/escape");
        pausarAction.AddBinding("<Keyboard>/p");
        pausarAction.AddBinding("<Gamepad>/start");
        pausarAction.Enable();
    }

    void OnDestroy()
    {
        pausarAction.Dispose();
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        Time.timeScale = 1f;
        estado = Estado.Jogando;
        tempoRestante = tempoInicial;
        total = GameObject.FindGameObjectsWithTag("Coletavel").Length;
        pausePanel.SetActive(false);
        fimPanel.SetActive(false);
        Aviso("Recolha as " + total + " shurikens antes que o tempo acabe!", 3f);
        AtualizarHud();
    }

    void Update()
    {
        if (pausarAction.WasPressedThisFrame())
        {
            if (estado == Estado.Jogando) Pausar();
            else if (estado == Estado.Pausado) Continuar();
        }

        // Mantém um botão selecionado para navegar com o controle
        GameObject painel = estado == Estado.Pausado ? primeiroBotaoPause : estado == Estado.Fim ? primeiroBotaoFim : null;
        if (painel != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            EventSystem.current.SetSelectedGameObject(painel);

        if (avisoTimer > 0f)
        {
            avisoTimer -= Time.unscaledDeltaTime;
            avisoText.enabled = avisoTimer > 0f;
        }

        if (estado != Estado.Jogando)
            return;

        // Cronômetro regressivo: o tempo é a principal ameaça
        tempoRestante -= Time.deltaTime;
        tempoDecorrido += Time.deltaTime;

        if (tempoRestante <= alertaTempo && tempoRestante > 0f)
        {
            int segundo = Mathf.CeilToInt(tempoRestante);
            if (segundo != ultimoTique)
            {
                ultimoTique = segundo;
                Som(somTique);
            }
        }

        if (tempoRestante <= 0f)
        {
            tempoRestante = 0f;
            AtualizarHud();
            Fim(false, "O tempo acabou!");
            return;
        }
        AtualizarHud();
    }

    // ---------- chamadas estáticas usadas pelos outros scripts ----------

    public static void Coletar()
    {
        if (!Jogando) return;
        Instance.coletados++;
        Instance.pontos += Instance.pontosShuriken;
        Instance.AtualizarHud();
        if (Instance.coletados >= Instance.total)
            Instance.Fim(true, "Todas as shurikens recolhidas!");
    }

    public static void GanharTempo()
    {
        if (!Jogando) return;
        Instance.tempoRestante += Instance.bonusAmpulheta;
        Instance.Aviso("+" + Instance.bonusAmpulheta.ToString("0") + " segundos!", 1.2f);
        Instance.AtualizarHud();
    }

    public static void AtualizarVida(int vida, int max)
    {
        if (Instance == null) return;
        for (int i = 0; i < Instance.coracoes.Length; i++)
        {
            Instance.coracoes[i].enabled = i < max;
            Instance.coracoes[i].sprite = i < vida ? Instance.coracaoCheio : Instance.coracaoVazio;
        }
    }

    public static void JogadorFerido(int vida, int max)
    {
        if (Instance == null) return;
        Som(Instance.somDano);
        AtualizarVida(vida, max);
        if (vida <= 0)
            Instance.Fim(false, "Você foi derrotado!");
    }

    public static void AtualizarStamina(float fracao)
    {
        if (Instance != null)
            Instance.staminaBarra.fillAmount = fracao;
    }

    public static void Som(AudioClip clip)
    {
        if (Instance != null && clip != null)
            Instance.sfx.PlayOneShot(clip);
    }

    // ---------- telas ----------

    void Fim(bool venceu, string motivo)
    {
        estado = Estado.Fim;
        Time.timeScale = 0f;

        int bonus = venceu ? Mathf.CeilToInt(tempoRestante) * pontosPorSegundoRestante : 0;
        pontos += bonus;
        AtualizarHud();

        fimTitulo.text = venceu ? "Missão cumprida!" : "Fim de jogo";
        fimTitulo.color = venceu ? new Color(1f, 0.85f, 0.3f) : new Color(0.9f, 0.25f, 0.25f);
        fimMotivo.text = motivo;
        fimStats.text =
            "Tempo da missão: " + FormatarTempo(tempoDecorrido, true) + "\n" +
            "Shurikens: " + coletados + " / " + total + "\n" +
            (venceu ? "Bônus de tempo: +" + bonus + "\n" : "") +
            "Pontuação final: " + pontos;

        musica.Stop();
        Som(venceu ? somVitoria : somDerrota);
        fimPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(primeiroBotaoFim);
    }

    public void Pausar()
    {
        estado = Estado.Pausado;
        Time.timeScale = 0f;
        musica.volume = 0.25f;
        Som(somClique);
        pausePanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(primeiroBotaoPause);
    }

    public void Continuar()
    {
        estado = Estado.Jogando;
        Time.timeScale = 1f;
        musica.volume = 0.6f;
        Som(somClique);
        pausePanel.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void JogarNovamente()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void VoltarAoMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuInicial");
    }

    // ---------- HUD ----------

    void AtualizarHud()
    {
        tempoText.text = FormatarTempo(tempoRestante, false);
        tempoText.color = tempoRestante <= alertaTempo ? new Color(1f, 0.3f, 0.3f) : Color.white;
        pontosText.text = "Pontos: " + pontos;
        shurikensText.text = "Shurikens: " + coletados + " / " + total;
    }

    void Aviso(string texto, float duracao)
    {
        avisoText.text = texto;
        avisoText.enabled = true;
        avisoTimer = duracao;
    }

    static string FormatarTempo(float segundos, bool decimos)
    {
        if (!decimos)
            segundos = Mathf.Ceil(segundos);
        int min = (int)(segundos / 60f);
        float resto = segundos - min * 60;
        return decimos ? min.ToString("00") + ":" + resto.ToString("00.0") : min.ToString("00") + ":" + ((int)resto).ToString("00");
    }
}
