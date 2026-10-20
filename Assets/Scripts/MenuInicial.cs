using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Menu inicial (passo 16): jogar, como jogar e sair.
public class MenuInicial : MonoBehaviour
{
    public GameObject painelPrincipal;
    public GameObject painelComoJogar;
    public GameObject botaoJogar;
    public GameObject botaoVoltar;
    public GameObject botaoSair;
    public AudioSource sfx;
    public AudioClip somClique;

    void Start()
    {
        Time.timeScale = 1f;
        painelComoJogar.SetActive(false);
#if UNITY_WEBGL
        // No navegador não existe "sair do jogo"
        botaoSair.SetActive(false);
#endif
        EventSystem.current.SetSelectedGameObject(botaoJogar);
    }

    void Update()
    {
        // Mantém um botão selecionado para navegar com o controle
        if (EventSystem.current.currentSelectedGameObject == null)
            EventSystem.current.SetSelectedGameObject(painelComoJogar.activeSelf ? botaoVoltar : botaoJogar);
    }

    public void Jogar()
    {
        SceneManager.LoadScene("Jogo");
    }

    public void ComoJogar()
    {
        sfx.PlayOneShot(somClique);
        painelPrincipal.SetActive(false);
        painelComoJogar.SetActive(true);
        EventSystem.current.SetSelectedGameObject(botaoVoltar);
    }

    public void Voltar()
    {
        sfx.PlayOneShot(somClique);
        painelComoJogar.SetActive(false);
        painelPrincipal.SetActive(true);
        EventSystem.current.SetSelectedGameObject(botaoJogar);
    }

    public void Sair()
    {
        Application.Quit();
    }
}
