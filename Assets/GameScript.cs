using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using TMPro;

public class GameScript : MonoBehaviour
{
    [SerializeField] PointScript pontuacao;
    [SerializeField] BallScipt bola;
    [SerializeField] CamsScript cams;
    [SerializeField] float esperaPinos = 2f;
    [SerializeField] bool iniciarNoMenu = true;
    [SerializeField] bool modoVsIA = true;
    [SerializeField] int totalQuadros = 10;

    [Header("UI")]
    [SerializeField] GameObject painelResultado;
    [SerializeField] TMP_Text textoPontos;
    [SerializeField] GameObject painelPlacarTopo; // Painel do placar no topo (só aparece em jogo)
    [SerializeField] TMP_Text textoTurno;         // Texto dentro do painelPlacarTopo

    [Header("Oponente IA (Robola)")]
    [SerializeField] float atrasoJogadaIA = 1.5f;
    [SerializeField, Range(0f, 1f)] float precisaoIA = 0.7f;
    [SerializeField] bool tocarVideosParaIA = true;

    [Header("Configuração do VideoPlayer")]
    [SerializeField] GameObject painelVideo;          // RawImage (ou painel pai) no Canvas
    [SerializeField] VideoPlayer videoPlayer;         // Componente VideoPlayer
    [SerializeField] bool permitirPularComClique = true;
    [SerializeField] bool ordemAleatoriaVideos = false; // Desmarcado = cicla em ordem (1, 2, 3...)

    [Header("Listas de Vídeos (Ciclo Automático)")]
    [Tooltip("1º Strike")]
    [SerializeField] VideoClip[] videosStrike;

    [Tooltip("2 Strikes seguidos (se vazio, usa os vídeos de Strike)")]
    [SerializeField] VideoClip[] videosDouble;

    [Tooltip("3 ou mais Strikes seguidos (se vazio, usa os vídeos de Strike)")]
    [SerializeField] VideoClip[] videosTurkey;

    [Tooltip("Spare: derrubou os 10 pinos nos 2 arremessos")]
    [SerializeField] VideoClip[] videosSpare;

    [Tooltip("Ficou faltando apenas 1 pino (9 derrubados) e errou no fim do quadro")]
    [SerializeField] VideoClip[] videosQuaseSpare;

    [Tooltip("Fim de quadro normal: não fechou Strike nem Spare (1 a 9 pinos)")]
    [SerializeField] VideoClip[] videosFimDeQuadro;

    [Tooltip("Errou todas as bolas: 0 pinos derrubados no quadro inteiro")]
    [SerializeField] VideoClip[] videosGutter;

    int quadroAtual = 1;
    int arremessoAtual = 1;      // 1 ou 2
    int derrubadosQuadro;        // soma dos dois arremessos
    int strikesSeguidosJogador;
    int strikesSeguidosIA;

    bool turnoIA;
    bool aguardandoContinuar;
    bool videoTerminou;

    public bool JogoIniciado { get; private set; }
    public bool ModoVsIA => modoVsIA;

    readonly Dictionary<string, int> indicesCiclo = new Dictionary<string, int>();

    void Awake()
    {
        if (painelVideo != null)
            painelVideo.SetActive(false);

        if (videoPlayer != null)
            videoPlayer.loopPointReached += AoTerminarVideo;

        if (iniciarNoMenu)
        {
            JogoIniciado = false;
            if (bola != null) bola.BloquearInputJogador = true;
            if (painelPlacarTopo != null) painelPlacarTopo.SetActive(false);
        }
        else
        {
            JogoIniciado = true;
            if (bola != null) bola.BloquearInputJogador = false;
            AtualizarTextoHUD();
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= AoTerminarVideo;
    }

    public void IniciarPartida(bool vsIA)
    {
        modoVsIA = vsIA;
        JogoIniciado = true;
        ResetarTudo();
    }

    public void PausarParaMenu()
    {
        JogoIniciado = false;
        ResetarTudo();
        if (bola != null)
            bola.BloquearInputJogador = true;
    }

    void AoTerminarVideo(VideoPlayer vp)
    {
        videoTerminou = true;
    }

    public void FimDaJogada()
    {
        StartCoroutine(ProximoArremesso());
    }

    IEnumerator ProximoArremesso()
    {
        yield return new WaitForSeconds(esperaPinos);   // pinos assentam

        int derrubados = pontuacao.VerificarERemover(turnoIA);
        derrubadosQuadro += derrubados;

        bool strike = arremessoAtual == 1 && derrubadosQuadro >= 10;
        bool fimDoQuadro = arremessoAtual == 2 || strike;

        if (!fimDoQuadro)
        {
            // 1º arremesso sem strike: segue direto para o 2º arremesso
            arremessoAtual = 2;
            bola.NovoArremesso();
            if (cams != null) cams.IrParaCamera(0);
            AtualizarTextoHUD();

            if (turnoIA)
                StartCoroutine(ExecutarJogadaIA());

            yield break;
        }

        // Atualiza sequência de strikes do jogador atual
        int strikesAtuais;
        if (turnoIA)
        {
            strikesSeguidosIA = strike ? strikesSeguidosIA + 1 : 0;
            strikesAtuais = strikesSeguidosIA;
        }
        else
        {
            strikesSeguidosJogador = strike ? strikesSeguidosJogador + 1 : 0;
            strikesAtuais = strikesSeguidosJogador;
        }

        bool spare = !strike && derrubadosQuadro >= 10;
        string titulo = ObterTitulo(strike, spare, strikesAtuais);
        VideoClip clipe = EscolherProximoVideo(strike, spare, strikesAtuais);

        if ((!turnoIA || tocarVideosParaIA) && clipe != null && videoPlayer != null && painelVideo != null)
            yield return StartCoroutine(ReproduzirVideo(clipe));

        bool fimDePartida = quadroAtual >= totalQuadros && (!modoVsIA || turnoIA);

        string quemJogou = turnoIA ? "ROBOLA" : "JOGADOR";
        if (fimDePartida)
        {
            string vencedor = ObterTextoVencedor();
            textoPontos.text = $"{vencedor}\nJogador: {pontuacao.TotalJogador}  |  Robola: {pontuacao.TotalIA}";
        }
        else if (modoVsIA)
        {
            textoPontos.text = $"[{quemJogou} - Quadro {quadroAtual}/{totalQuadros}]\n{titulo}\nPinos no quadro: {derrubadosQuadro}\n\nJogador: {pontuacao.TotalJogador}  |  Robola: {pontuacao.TotalIA}";
        }
        else
        {
            textoPontos.text = $"[Quadro {quadroAtual}/{totalQuadros}]\n{titulo}\nPinos no quadro: {derrubadosQuadro}\nTotal: {pontuacao.TotalJogador}";
        }

        painelResultado.SetActive(true);

        aguardandoContinuar = true;
        yield return new WaitUntil(() => !aguardandoContinuar);

        painelResultado.SetActive(false);

        if (fimDePartida)
        {
            ResetarTudo();
            yield break;
        }

        // Alterna entre Jogador e Robola
        if (modoVsIA)
        {
            turnoIA = !turnoIA;
            if (!turnoIA)
                quadroAtual++;
        }
        else
        {
            quadroAtual++;
        }

        arremessoAtual = 1;
        derrubadosQuadro = 0;
        pontuacao.RestaurarPinos();
        bola.NovoArremesso();
        if (cams != null) cams.IrParaCamera(0);

        bola.BloquearInputJogador = !JogoIniciado || turnoIA;
        AtualizarTextoHUD();

        if (turnoIA)
        {
            Debug.Log("[Robola] Iniciando turno da IA...");
            StartCoroutine(ExecutarJogadaIA());
        }
    }

    IEnumerator ExecutarJogadaIA()
    {
        bola.BloquearInputJogador = true;
        AtualizarTextoHUD();

        yield return new WaitForSeconds(atrasoJogadaIA);

        float centroX = (bola.MinX + bola.MaxX) * 0.5f;
        float metade = (bola.MaxX - bola.MinX) * 0.5f;

        float desvioMax = metade * Mathf.Lerp(1f, 0.25f, precisaoIA);
        float posX = centroX + Random.Range(-desvioMax, desvioMax);

        float fatorMin = Mathf.Lerp(0.1f, 0.45f, precisaoIA);
        float forcaIA = Mathf.Lerp(bola.ForcaMin, bola.ForcaMax, Random.Range(fatorMin, 1f));

        Debug.Log($"[Robola] Lançando bola com força {forcaIA:F1} em X={posX:F2}");
        bola.LancarComForca(forcaIA, posX);
    }

    string ObterTextoVencedor()
    {
        if (!modoVsIA) return $"FIM DE JOGO! Total: {pontuacao.TotalJogador}";
        if (pontuacao.TotalJogador > pontuacao.TotalIA) return "VITÓRIA DO JOGADOR!";
        if (pontuacao.TotalIA > pontuacao.TotalJogador) return "ROBOLA VENCEU!";
        return "EMPATE!";
    }

    void AtualizarTextoHUD()
    {
        if (painelPlacarTopo != null)
            painelPlacarTopo.SetActive(JogoIniciado);

        if (textoTurno == null) return;

        if (modoVsIA)
        {
            string vezDe = turnoIA ? "Robola" : $"Jogador (Bola {arremessoAtual})";
            textoTurno.text = $"Jogador  x  Robola\n{pontuacao.TotalJogador}  -  {pontuacao.TotalIA}\nVez de {vezDe}";
        }
        else
        {
            textoTurno.text = $"Jogador (Quadro {quadroAtual}/{totalQuadros})\n{pontuacao.TotalJogador} pts\nVez de Jogador (Bola {arremessoAtual})";
        }
    }

    string ObterTitulo(bool strike, bool spare, int strikesAtuais)
    {
        if (strike)
        {
            if (strikesAtuais >= 3) return "TURKEY!";
            if (strikesAtuais == 2) return "DOUBLE STRIKE!";
            return "STRIKE!";
        }

        if (spare) return "SPARE!";
        if (derrubadosQuadro == 0) return "ZEROU O QUADRO!";
        if (derrubadosQuadro == 9) return "QUASE LÁ! (9 PINOS)";
        return "Fim do quadro";
    }

    VideoClip EscolherProximoVideo(bool strike, bool spare, int strikesAtuais)
    {
        if (strike)
        {
            if (strikesAtuais >= 3 && TemVideos(videosTurkey))
                return ObterDoCiclo("turkey", videosTurkey);

            if (strikesAtuais == 2 && TemVideos(videosDouble))
                return ObterDoCiclo("double", videosDouble);

            return ObterDoCiclo("strike", videosStrike);
        }

        if (spare)
            return ObterDoCiclo("spare", videosSpare);

        if (derrubadosQuadro == 0)
            return ObterDoCiclo("gutter", videosGutter);

        if (derrubadosQuadro == 9 && TemVideos(videosQuaseSpare))
            return ObterDoCiclo("quase_spare", videosQuaseSpare);

        return ObterDoCiclo("fim_quadro", videosFimDeQuadro);
    }

    bool TemVideos(VideoClip[] lista)
    {
        if (lista == null || lista.Length == 0) return false;
        for (int i = 0; i < lista.Length; i++)
        {
            if (lista[i] != null) return true;
        }
        return false;
    }

    VideoClip ObterDoCiclo(string chave, VideoClip[] lista)
    {
        if (!TemVideos(lista)) return null;

        if (ordemAleatoriaVideos)
        {
            for (int tentativa = 0; tentativa < 10; tentativa++)
            {
                VideoClip sorteado = lista[Random.Range(0, lista.Length)];
                if (sorteado != null) return sorteado;
            }
        }

        indicesCiclo.TryGetValue(chave, out int indiceAtual);

        for (int i = 0; i < lista.Length; i++)
        {
            int idx = (indiceAtual + i) % lista.Length;
            if (lista[idx] != null)
            {
                indicesCiclo[chave] = (idx + 1) % lista.Length;
                return lista[idx];
            }
        }

        return null;
    }

    IEnumerator ReproduzirVideo(VideoClip clipe)
    {
        bola.BloquearInputJogador = true;
        videoTerminou = false;

        if (videoPlayer.targetTexture != null)
            videoPlayer.targetTexture.Release();

        videoPlayer.clip = clipe;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
            yield return null;

        painelVideo.SetActive(true);
        videoPlayer.Play();

        while (!videoTerminou)
        {
            if (permitirPularComClique && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                break;

            yield return null;
        }

        videoPlayer.Stop();
        painelVideo.SetActive(false);
        bola.BloquearInputJogador = !JogoIniciado || turnoIA;
    }

    // Ligado ao OnClick do botão Continuar
    public void Continuar()
    {
        aguardandoContinuar = false;
    }

    void Update()
    {
        if (!JogoIniciado) return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            ResetarTudo();
    }

    public void ResetarTudo()
    {
        StopAllCoroutines();

        if (videoPlayer != null) videoPlayer.Stop();
        if (painelVideo != null) painelVideo.SetActive(false);

        turnoIA = false;
        quadroAtual = 1;
        arremessoAtual = 1;
        derrubadosQuadro = 0;
        strikesSeguidosJogador = 0;
        strikesSeguidosIA = 0;
        aguardandoContinuar = false;

        bola.BloquearInputJogador = !JogoIniciado;
        pontuacao.Resetar();
        bola.Resetar();
        if (cams != null) cams.IrParaCamera(0);
        painelResultado.SetActive(false);
        AtualizarTextoHUD();
    }
}