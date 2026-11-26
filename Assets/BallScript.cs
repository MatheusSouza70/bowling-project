using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BallScipt : MonoBehaviour
{
    [SerializeField] Vector3 direcao = Vector3.forward;
    [SerializeField] float limiteY = -1f;
    [SerializeField] Camera cam;

    [SerializeField] PointScript pontuacao;

    [SerializeField] float velocidadeLateral = 1.5f;
    [SerializeField] float minX = 5.8f;
    [SerializeField] float maxX = 6.6f;
    [SerializeField] float zInicio = -13f;
    [SerializeField] float zFim = 0.24f;

    [Header("Percurso")]
    [SerializeField] float desaceleracao = 0.15f;     // m/s² contra o movimento
    [SerializeField] float forcaDesvio = 0.6f;        // m/s² lateral no extremo da pista
    [SerializeField] float expoenteCurva = 2f;        // maior = desvio mais tardio

    [Header("Força e Curva")]
    [SerializeField] float forcaMin = 8f;
    [SerializeField] float forcaMax = 16f;
    [SerializeField, Range(0f, 1f)] float chanceVoltarCentro = 0.6f; // 0 = nunca, 1 = sempre

    [Header("Barra de Força (UI)")]
    [SerializeField] GameObject painelBarraForca;     // Objeto pai da barra no Canvas
    [SerializeField] Slider barraForcaSlider;         // Slider (Min Value = 0, Max Value = 1)
    [SerializeField] Image barraForcaFill;            // Image (Fill) para mudar de cor conforme a força
    [SerializeField] float velocidadeBarra = 1.6f;    // Velocidade de oscilação (vai e volta)
    [SerializeField] Color corForcaMin = new Color(0.2f, 0.85f, 0.3f); // Verde
    [SerializeField] Color corForcaMedia = new Color(1f, 0.85f, 0.1f); // Amarelo
    [SerializeField] Color corForcaMax = new Color(0.95f, 0.2f, 0.2f); // Vermelho

    [Header("Referências do Jogo")]
    [SerializeField] GameScript jogo;                 // arraste o GerenciadorJogo
    [SerializeField] float tempoParada = 1.5f;        // tempo parada até encerrar

    // Controle interno de estado
    float tempoSemMover;
    bool encerrada;
    float sinalDesvio = -1f;                          // -1 = volta ao centro, +1 = vai para fora
    float centroX;                                    // centro da pista
    float fatorLateral;                               // -1 (esquerdo) a +1 (direito), fixado no lançamento
    bool lancada;

    // Controle da barra de força ao segurar o clique
    bool carregandoForca;
    float tempoCarregando;
    float percentualForcaAtual;

    // Permite bloquear o clique/teclado durante o turno da IA ou no Menu
    public bool BloquearInputJogador { get; set; }

    public float ForcaMin => forcaMin;
    public float ForcaMax => forcaMax;
    public float MinX => minX;
    public float MaxX => maxX;

    Rigidbody rb;
    Vector3 posInicial;
    Quaternion rotInicial;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        posInicial = transform.position;
        posInicial.x = Mathf.Clamp(posInicial.x, minX, maxX);
        transform.position = posInicial;
        rotInicial = transform.rotation;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (cam == null) cam = Camera.main;

        if (cam == null)
            Debug.LogError("Nenhuma câmera ativa encontrada na cena!", this);

        ResetarBarraUI();
    }

    public void Resetar()
    {
        NovoArremesso();
    }

    public void NovoArremesso()
    {
        gameObject.SetActive(true);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(posInicial, rotInicial);
        lancada = false;
        encerrada = false;
        carregandoForca = false;
        tempoSemMover = 0f;
        tempoCarregando = 0f;
        percentualForcaAtual = 0f;
        fatorLateral = 0f;
        sinalDesvio = -1f;

        ResetarBarraUI();
    }

    void ResetarBarraUI()
    {
        AtualizarVisualBarra(0f);
        if (painelBarraForca != null)
            painelBarraForca.SetActive(false);
    }

    void Update()
    {
        if (transform.position.y < limiteY)
        {
            gameObject.SetActive(false);
            if (lancada && !encerrada)
            {
                encerrada = true;
                jogo.FimDaJogada();
            }
            return;
        }

        if (lancada || BloquearInputJogador) return;

        // Só move para os lados quando ainda não estiver segurando o clique de força
        if (!carregandoForca)
            MoverLateral();

        if (cam == null || Mouse.current == null) return;

        // 1. Começou a segurar o clique em cima da bola
        if (!carregandoForca && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            {
                carregandoForca = true;
                tempoCarregando = 0f;
                percentualForcaAtual = 0f;

                if (painelBarraForca != null)
                    painelBarraForca.SetActive(true);

                AtualizarVisualBarra(0f);
            }
        }

        // 2. Está segurando o clique: anima a barra em PingPong (0 -> 1 -> 0)
        if (carregandoForca && Mouse.current.leftButton.isPressed)
        {
            tempoCarregando += Time.deltaTime * velocidadeBarra;
            percentualForcaAtual = Mathf.PingPong(tempoCarregando, 1f);
            AtualizarVisualBarra(percentualForcaAtual);
        }

        // 3. Soltou o clique: calcula a força escolhida e lança a bola
        if (carregandoForca && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            carregandoForca = false;
            float forcaEscolhida = Mathf.Lerp(forcaMin, forcaMax, percentualForcaAtual);

            if (painelBarraForca != null)
                painelBarraForca.SetActive(false);

            DispararBola(forcaEscolhida);
        }
    }

    void AtualizarVisualBarra(float percentual)
    {
        if (barraForcaSlider != null)
            barraForcaSlider.value = percentual;

        if (barraForcaFill != null)
        {
            // Se o usuário usar uma Image do tipo Filled sem Slider, também atualiza o fillAmount
            if (barraForcaFill.type == Image.Type.Filled)
                barraForcaFill.fillAmount = percentual;

            // Gradiente suave: Verde (0) -> Amarelo (0.5) -> Vermelho (1.0)
            barraForcaFill.color = percentual < 0.5f
                ? Color.Lerp(corForcaMin, corForcaMedia, percentual * 2f)
                : Color.Lerp(corForcaMedia, corForcaMax, (percentual - 0.5f) * 2f);
        }
    }

    // Método público que também poderá ser chamado pela IA na próxima etapa
    public void LancarComForca(float forca, float? posicaoX = null)
    {
        if (lancada) return;

        if (posicaoX.HasValue)
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(posicaoX.Value, minX, maxX);
            transform.position = p;
        }

        DispararBola(Mathf.Clamp(forca, forcaMin, forcaMax));
    }

    void DispararBola(float forcaAplicada)
    {
        lancada = true;
        centroX = (minX + maxX) * 0.5f;
        float metade = (maxX - minX) * 0.5f;
        fatorLateral = Mathf.Clamp((transform.position.x - centroX) / metade, -1f, 1f);

        sinalDesvio = Random.value < chanceVoltarCentro ? -1f : 1f;

        rb.AddForce(direcao.normalized * forcaAplicada, ForceMode.Impulse);
        Debug.Log($"Força aplicada: {forcaAplicada:F1} ({percentualForcaAtual * 100f:F0}%) | Volta ao centro: {sinalDesvio < 0f}");
    }

    void MoverLateral()
    {
        if (Keyboard.current == null) return;

        float entrada = 0f;
        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) entrada -= 1f;
        if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) entrada += 1f;

        if (entrada == 0f) return;

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x + entrada * velocidadeLateral * Time.deltaTime, minX, maxX);
        transform.position = p;
    }

    void FixedUpdate()
    {
        if (!lancada) return;

        Vector3 v = rb.linearVelocity;

        if (!encerrada)
        {
            tempoSemMover = rb.linearVelocity.sqrMagnitude < 0.01f ? tempoSemMover + Time.fixedDeltaTime : 0f;
            if (tempoSemMover >= tempoParada)
            {
                encerrada = true;
                jogo.FimDaJogada();
            }
        }

        if (v.sqrMagnitude < 0.01f) return;

        Vector3 vh = new Vector3(v.x, 0f, v.z);
        rb.AddForce(-vh.normalized * desaceleracao, ForceMode.Acceleration);

        float sinal = sinalDesvio;
        float progresso = Mathf.InverseLerp(zInicio, zFim, transform.position.z);
        float curva = Mathf.Pow(progresso, expoenteCurva);

        rb.AddForce(Vector3.right * fatorLateral * forcaDesvio * sinal * curva, ForceMode.Acceleration);
    }
}