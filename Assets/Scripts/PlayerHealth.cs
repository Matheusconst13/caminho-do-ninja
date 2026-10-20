using UnityEngine;

// Vida do ninja: dano por inimigos e armadilhas, empurrão e invencibilidade temporária.
public class PlayerHealth : MonoBehaviour
{
    public int vidaMax = 3;
    public float invencivelDuracao = 1.2f;
    public float forcaEmpurrao = 9f;

    private int vida;
    private float invencivelTimer;
    private SpriteRenderer sr;
    private PlayerMovement movimento;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        movimento = GetComponent<PlayerMovement>();
        vida = vidaMax;
        invencivelTimer = 1f;   // proteção no início da partida
        GameManager.AtualizarVida(vida, vidaMax);
    }

    void Update()
    {
        if (invencivelTimer > 0f)
        {
            invencivelTimer -= Time.deltaTime;
            // Pisca enquanto está invencível
            sr.enabled = invencivelTimer <= 0f || Mathf.Repeat(invencivelTimer, 0.16f) > 0.08f;
        }
        else
        {
            sr.enabled = true;
        }
    }

    // Samurais e o ninja sombra são sólidos
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Inimigo"))
            TomarDano(collision.transform.position);
    }

    // Estrepes (makibishi) são triggers
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Armadilha"))
            TomarDano(other.transform.position);
    }

    void TomarDano(Vector3 origem)
    {
        if (invencivelTimer > 0f || movimento.EmDash || !GameManager.Jogando)
            return;

        vida--;
        invencivelTimer = invencivelDuracao;

        Vector2 direcao = (Vector2)(transform.position - origem);
        if (direcao.sqrMagnitude < 0.0001f)
            direcao = Vector2.up;
        movimento.Empurrar(direcao.normalized * forcaEmpurrao, 0.15f);

        GameManager.JogadorFerido(vida, vidaMax);
    }
}
