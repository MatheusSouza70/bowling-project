using UnityEngine;
using UnityEngine.InputSystem;

public class MenuScript : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] GameScript jogo;
    [SerializeField] CamsScript cams;
    [SerializeField] int indiceCameraMenu = 0;

    [Header("Painéis de UI")]
    [SerializeField] GameObject painelMenu;
    [SerializeField] GameObject painelCreditos;
    [SerializeField] GameObject hudJogo; // Opcional: HUD exibida só durante a partida

    void Start()
    {
        MostrarMenu();
    }

    void Update()
    {
        // Pressionar ESC durante a partida volta para o Menu
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (painelCreditos != null && painelCreditos.activeSelf)
                FecharCreditos();
            else if (jogo != null && jogo.JogoIniciado)
                VoltarAoMenu();
        }
    }

    public void MostrarMenu()
    {
        if (painelMenu != null) painelMenu.SetActive(true);
        if (painelCreditos != null) painelCreditos.SetActive(false);
        if (hudJogo != null) hudJogo.SetActive(false);

        if (cams != null)
            cams.IrParaCamera(indiceCameraMenu);

        if (jogo != null)
            jogo.PausarParaMenu();
    }

    // Conecte ao botão "Iniciar Jogo (vs Computador)"
    public void IniciarJogoVsComputador()
    {
        if (painelMenu != null) painelMenu.SetActive(false);
        if (painelCreditos != null) painelCreditos.SetActive(false);
        if (hudJogo != null) hudJogo.SetActive(true);

        if (jogo != null)
            jogo.IniciarPartida(true);
    }

    // Opcional: caso queira um botão extra de "Treino Solo"
    public void IniciarJogoSolo()
    {
        if (painelMenu != null) painelMenu.SetActive(false);
        if (painelCreditos != null) painelCreditos.SetActive(false);
        if (hudJogo != null) hudJogo.SetActive(true);

        if (jogo != null)
            jogo.IniciarPartida(false);
    }

    // Conecte ao botão "Créditos"
    public void AbrirCreditos()
    {
        if (painelMenu != null) painelMenu.SetActive(false);
        if (painelCreditos != null) painelCreditos.SetActive(true);
    }

    // Conecte ao botão "Voltar" dentro da tela de Créditos
    public void FecharCreditos()
    {
        if (painelCreditos != null) painelCreditos.SetActive(false);
        if (painelMenu != null) painelMenu.SetActive(true);
    }

    // Conecte a um botão "Menu" no HUD ou no painel de fim de quadro
    public void VoltarAoMenu()
    {
        MostrarMenu();
    }

    // Conecte ao botão "Fechar Jogo"
    public void FecharJogo()
    {
        Debug.Log("Fechando o jogo...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}