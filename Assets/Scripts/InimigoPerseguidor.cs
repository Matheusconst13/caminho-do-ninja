using UnityEngine;

// Ninja sombra: patrulha entre dois pontos e persegue o jogador quando ele chega perto.
public class InimigoPerseguidor : MonoBehaviour
{
    public Vector2 pontoA;
    public Vector2 pontoB;
    public float velocidadePatrulha = 1.6f;
    public float velocidadePerseguicao = 2.9f;
    public float raioDeteccao = 4f;
    public float raioDesistir = 6.5f;
    public AudioClip somAlerta;
    public SpriteRenderer areaDeteccao;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform jogador;
    private Vector2 alvo;
    private bool perseguindo;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        jogador = GameObject.FindGameObjectWithTag("Player").transform;
        alvo = pontoB;
    }

    void FixedUpdate()
    {
        if (!GameManager.Jogando)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distancia = Vector2.Distance(rb.position, jogador.position);
        if (!perseguindo && distancia < raioDeteccao)
        {
            perseguindo = true;
            GameManager.Som(somAlerta);
        }
        else if (perseguindo && distancia > raioDesistir)
        {
            perseguindo = false;
        }

        if (!perseguindo && Vector2.Distance(rb.position, alvo) < 0.15f)
            alvo = alvo == pontoA ? pontoB : pontoA;

        Vector2 destino = perseguindo ? (Vector2)jogador.position : alvo;
        Vector2 direcao = destino - rb.position;
        float velocidade = perseguindo ? velocidadePerseguicao : velocidadePatrulha;
        rb.linearVelocity = direcao.sqrMagnitude > 0.0001f ? direcao.normalized * velocidade : Vector2.zero;

        if (Mathf.Abs(direcao.x) > 0.05f)
            sr.flipX = direcao.x < 0f;
        sr.color = perseguindo ? new Color(1f, 0.7f, 0.7f) : Color.white;
        if (areaDeteccao != null)
            areaDeteccao.color = perseguindo ? new Color(1f, 0.2f, 0.2f, 0.18f) : new Color(0.6f, 0.3f, 1f, 0.12f);
    }
}
