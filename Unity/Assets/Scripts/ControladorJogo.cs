using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections.Generic;

public class ControladorJogo : NetworkBehaviour
{
	[Header("Jogadores")]
	public List<GameObject> canvasJogadores;
	internal GameObject[] jogadores;
	private List<Jogador> jogadoresScripts = new List<Jogador>();
	public float delayExibirEstatisticas;
	public float delayOcultarEstatisticas;
	public float duracaoAnimacaoCanvas;
	public float duracaoAnimacaoTempo;
	public float duracaoAnimacaoConcentracao;

	[Header("Corrida")]
	public int numeroJogadoresNecessario;
	public int voltasTotal;
	public GameObject posicionamentoText;

	[SyncVar]
	internal bool corridaIniciada;

	[SyncVar]
	internal float tempoInicioCorrida;

	[SyncVar]
	internal float tempoFinalCorrida;

	[SyncVar]
	internal float tempoServidor;
	internal List<Text> voltasTotalTextJogadores = new List<Text>();

	[Header("Vencedor")]
	public Image vencedorImage;
	private Animator vencedorAnimator;
	public List<Sprite> trofeus;
	internal List<Image> premioImageJogadores = new List<Image>();

	[Header("Fade")]
	public Animator fadeAnimator;

	[SyncVar]
	internal bool jogoIniciado;

	[SyncVar]
	internal bool vencedorDeclarado;

	[SyncVar]
	internal int vencedorCorrida;

	private ControladorConexoes controladorConexoes;

	private void Start()
	{
		fadeAnimator.SetTrigger("FadeIn");

		controladorConexoes = ControladorConexoes.Pegar();
		controladorConexoes.networkManagerHUD.showGUI = false;

		int voltasSelecionadas = controladorConexoes.voltasSelecionadas;
		if (voltasSelecionadas > 0 && voltasSelecionadas != voltasTotal)
			voltasTotal = voltasSelecionadas;

		vencedorAnimator = vencedorImage.GetComponent<Animator>();
		AlterarExibicaoVencedor(false);

		PegarPremioImageJogadores();
		AtualizarVoltasTotalTextJogadores();

		AlterarPosicionamento();
	}

	private void Update()
	{
		if (isServer)
			tempoServidor = Time.time;

		if (!jogoIniciado)
		{
			jogadores = GameObject.FindGameObjectsWithTag("Player");

			if (jogadores.Length == numeroJogadoresNecessario)
				IniciarJogo();
		}
		else if (corridaIniciada)
		{
			ChecarVencedorParcial();
			AtualizarVencedor();
		}
	}

	private void AtualizarVoltasTotalTextJogadores()
	{
		foreach (GameObject canvasJogador in canvasJogadores)
		{
			Transform canvasJogadorCorrida = canvasJogador.transform.FindChild("Corrida");
			Text voltasTotalTextJogador = canvasJogadorCorrida.FindChild("VoltasTotal").GetComponent<Text>();
			voltasTotalTextJogador.text = string.Format("/{0:00}", voltasTotal);
		}
	}

	private void PegarPremioImageJogadores()
	{
		foreach (GameObject canvasJogador in canvasJogadores)
		{
			Transform canvasJogadorCorrida = canvasJogador.transform.FindChild("Corrida");
			Image premioImageJogador = canvasJogadorCorrida.FindChild("Prêmio").GetComponent<Image>();
			premioImageJogadores.Add(premioImageJogador);
			premioImageJogador.gameObject.SetActive(false);
		}
	}

	private void AtualizarVencedor()
	{
		if (vencedorCorrida > 0)
		{
			if (!vencedorImage.gameObject.activeSelf)
			{
				AlterarExibicaoVencedor(true);
				vencedorAnimator.SetTrigger("AparecerJogador" + vencedorCorrida);
			}
			else
			{
				vencedorAnimator.ResetTrigger("Jogador" + (vencedorCorrida == 1 ? 2 : 1));
				vencedorAnimator.SetTrigger("Jogador" + vencedorCorrida);
			}
		}
		else
		{
			vencedorAnimator.Stop();
			AlterarExibicaoVencedor(false);
		}
	}

	public void AlterarExibicaoVencedor(bool exibicao)
	{
		if (vencedorImage.gameObject.activeSelf != exibicao)
			vencedorImage.gameObject.SetActive(exibicao);
	}

	private void IniciarJogo()
	{
		jogoIniciado = true;

		jogadoresScripts.Clear();
		foreach (GameObject jogador in jogadores)
			jogadoresScripts.Add(jogador.GetComponent<Jogador>());
	}

	public void IniciarCorrida()
	{
		vencedorCorrida = 0;
		vencedorDeclarado = false;
		corridaIniciada = true;
		tempoInicioCorrida = Time.time;

		foreach (Jogador jogadorScript in jogadoresScripts)
		{
			jogadorScript.OcultarEstatisticas();
			jogadorScript.estatisticasLiberadas = true;
			jogadorScript.ZerarEstatisticas();
			jogadorScript.PrepararInicioCorrida();
		}
	}

	public void EncerrarCorrida()
	{
		tempoFinalCorrida = Time.time;
		ChecarVencedorParcial();
		AtualizarVencedor();
		corridaIniciada = false;

		foreach (Jogador jogadorScript in jogadoresScripts)
			jogadorScript.CalcularEstatisticasConcentracao();
	}

	public void DeclararVencedor(int vencedor)
	{
		vencedorDeclarado = true;
		vencedorCorrida = vencedor;
	}

	private void ChecarVencedorParcial()
	{
		List<int> voltas = new List<int>();
		foreach (GameObject jogador in jogadores)
		{
			Jogador jogadorScript = jogador.GetComponent<Jogador>();
			voltas.Add(jogadorScript.voltas);
		}

		if (voltas[0] > voltas[1])
			vencedorCorrida = 1;
		else if (voltas[0] < voltas[1])
			vencedorCorrida = 2;
		else if (voltas[0] == 0 && voltas[1] == 0)
			vencedorCorrida = 0;
	}

	public void AlterarPosicionamento()
	{
		posicionamentoText.SetActive(!posicionamentoText.activeSelf);
	}

	static public ControladorJogo Pegar()
	{
		return GameObject.Find("ControladorJogo").GetComponent<ControladorJogo>();
	}
}
