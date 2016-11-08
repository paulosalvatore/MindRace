using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;

public class ControladorJogo : NetworkBehaviour
{
	[Header("Jogadores")]
	internal GameObject[] jogadores;
	public List<GameObject> canvasJogadores;

	[Header("Corrida")]
	public int numeroJogadoresNecessario;
	public int voltasTotal;
	[SyncVar] internal bool corridaIniciada;
	[SyncVar] internal float tempoInicioCorrida;
	[SyncVar] internal float tempoServidor;

	[Header("Ícones de Sinal")]
	public Sprite[] iconesSinal;

	[SyncVar] internal bool jogoIniciado;
	[SyncVar] internal bool vencedorDeclarado;
	[SyncVar] internal int vencedorCorrida;

	public Text vencedorText;

	// private float tempoFinalCorrida;
	// private List<float> tempoVoltas = new List<float>();

	void Start()
	{
	}

	void Update()
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

	void AtualizarVencedor()
	{
		string mensagemVencedor = "";
		if (vencedorCorrida > 0)
		{
			mensagemVencedor = vencedorCorrida.ToString();
		}
		else
		{
			mensagemVencedor = "Ninguém";
		}

		vencedorText.text = mensagemVencedor;
	}

	void IniciarJogo()
	{
		jogoIniciado = true;
	}

	public void IniciarCorrida()
	{
		vencedorDeclarado = false;
		corridaIniciada = true;
		tempoInicioCorrida = Time.time;
	}

	public void EncerrarCorrida()
	{
		corridaIniciada = false;
	}

	public void DeclararVencedor(int vencedor)
	{
		vencedorDeclarado = true;
		vencedorCorrida = vencedor;
		Debug.LogError("Vencedor da Corrida: " + vencedor);
	}

	void ChecarVencedorParcial()
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

	/*int PegarIdJogador(string nome)
	{
		return nome.Contains("1") ? 0 : 1;
	}*/

	static public ControladorJogo Pegar()
	{
		return GameObject.Find("ControladorJogo").GetComponent<ControladorJogo>();
	}
	/*
	private GameObject[] jogadores;
	public List<GameObject> canvasJogadores;
	public int numeroJogadoresNecessario;

	public Sprite[] iconesSinal;

	private bool jogoIniciado;

	private GameObject networkManager;
	
	public int voltasTotal;

	private bool corridaIniciada;
	private float tempoInicioCorrida;
	// private float tempoFinalCorrida;
	// private List<float> tempoVoltas = new List<float>();

	private List<Text> voltasTextJogadores = new List<Text>();
	private List<Text> tempoTextJogadores = new List<Text>();
	private List<Text> concentracaoTextJogadores = new List<Text>();
	private List<Image> sinalImageJogadores = new List<Image>();
	private List<Jogador> scriptJogadores = new List<Jogador>();

	void Start()
	{
		EsconderNetworkHUD();
	}

	void Update()
	{
		if (!jogoIniciado)
		{
			jogadores = GameObject.FindGameObjectsWithTag("Player");

			if (jogadores.Length == numeroJogadoresNecessario)
				IniciarJogo();
		}
		else if (corridaIniciada)
		{
			AtualizarJogadores();
		}
	}

	void IniciarJogo()
	{
		foreach (GameObject canvasJogador in canvasJogadores)
		{
			int idJogador = PegarIdJogador(canvasJogador.name);

			// Apenas durante os testes pra rodar com um player só e não dar erro
			if (jogadores.Length - 1 < idJogador)
				continue;

			Text voltasText = canvasJogador.transform.FindChild("Voltas").GetComponent<Text>();
			Text tempoText = canvasJogador.transform.FindChild("Tempo").GetComponent<Text>();
			Text concentracaoText = canvasJogador.transform.FindChild("Concentração").GetComponent<Text>();
			Image sinalImage = canvasJogador.transform.FindChild("Sinal").GetComponent<Image>();

			Jogador jogadorScript = jogadores[idJogador].GetComponent<Jogador>();

			voltasTextJogadores.Add(voltasText);
			tempoTextJogadores.Add(tempoText);
			concentracaoTextJogadores.Add(concentracaoText);
			sinalImageJogadores.Add(sinalImage);

			scriptJogadores.Add(jogadorScript);
		}

		jogoIniciado = true;
	}

	public void IniciarCorrida()
	{
		if (corridaIniciada)
			return;

		corridaIniciada = true;
		tempoInicioCorrida = Time.time;

		// inicioCorrida = Time.time;

		Debug.Log("IniciarCorrida");
	}

	int PegarIdJogador(string nome)
	{
		return nome.Contains("1") ? 0 : 1;
	}

	void AtualizarJogadores()
	{
		Debug.Log("Atualizar Jogadores");
		foreach (GameObject canvasJogador in canvasJogadores)
		{
			int idJogador = PegarIdJogador(canvasJogador.name);
			
			// Apenas durante os testes pra rodar com um player só e não dar erro
			if (jogadores.Length - 1 < idJogador)
				continue;

			Text voltasText = voltasTextJogadores[idJogador];
			Text tempoText = tempoTextJogadores[idJogador];
			Text concentracaoText = concentracaoTextJogadores[idJogador];
			Image sinalImage = sinalImageJogadores[idJogador];
			Jogador jogadorScript = scriptJogadores[idJogador];

			TimeSpan tempo = TimeSpan.FromSeconds(Time.time - tempoInicioCorrida);

			voltasText.text = jogadorScript.voltas + "/" + controladorJogo.voltasTotal;
			tempoText.text = string.Format("{0:00}:{1:00}:{2:00}", tempo.Minutes, tempo.Seconds, tempo.Milliseconds / 10);
			concentracaoText.text = jogadorScript.concentracao.ToString() + "%";
			sinalImage.sprite = iconesSinal[jogadorScript.indexIconeSinal];
		}
	}

	void EsconderNetworkHUD()
	{
		networkManager = GameObject.Find("Network Manager");

		if (networkManager)
			networkManager.GetComponent<NetworkManagerHUD>().showGUI = false;
	}

	static public ControladorJogo Pegar()
	{
		return GameObject.Find("ControladorJogo").GetComponent<ControladorJogo>();
	}
	*/
}
