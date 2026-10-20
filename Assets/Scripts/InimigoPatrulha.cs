using UnityEngine;

// Samurai: anda de um ponto ao outro, em direção pré-determinada (vai e volta).
public class InimigoPatrulha : MonoBehaviour
{
    public Vector2 pontoA;
    public Vector2 pontoB;
    public float velocidade = 2.5f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 alvo;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        alvo = pontoB;
    }

    void FixedUpdate()
    {
        Vector2 novaPosicao = Vector2.MoveTowards(rb.position, alvo, velocidade * Time.fixedDeltaTime);
        if (Mathf.Abs(novaPosicao.x - rb.position.x) > 0.001f)
            sr.flipX = novaPosicao.x < rb.position.x;
        rb.MovePosition(novaPosicao);

        if ((novaPosicao - alvo).sqrMagnitude < 0.0001f)
            alvo = alvo == pontoB ? pontoA : pontoB;
    }
}
