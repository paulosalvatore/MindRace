using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.IO.Ports;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;

public class Jogador : NetworkBehaviour
{
	private ControladorJogo controladorJogo;
	private TGCConnectionController controladorNeuroSky;
	private SerialPort arduino;

	[SyncVar] internal int voltas;
	[SyncVar] internal int concentracao;
	[SyncVar] internal int indexIconeSinal = 1;

	[SyncVar] private int sinal;

	[SyncVar] internal SyncListFloat tempoVoltas = new SyncListFloat();

	internal int idJogador;

	private Text voltasText;
	private Text melhorTempoText;
	private Text tempoText;
	private Text concentracaoText;
	private Image sinalImage;

	void PegarElementosCanvas()
	{
		int indexJogador = idJogador - 1;
		
		GameObject canvasJogador = controladorJogo.canvasJogadores[indexJogador];

		voltasText = canvasJogador.transform.FindChild("Voltas").GetComponent<Text>();
		tempoText = canvasJogador.transform.FindChild("Tempo").GetComponent<Text>();
		melhorTempoText = canvasJogador.transform.FindChild("MelhorTempo").GetComponent<Text>();
		concentracaoText = canvasJogador.transform.FindChild("Concentração").GetComponent<Text>();
		sinalImage = canvasJogador.transform.FindChild("Sinal").GetComponent<Image>();
	}

	void Start()
	{
		controladorJogo = ControladorJogo.Pegar();

		if (isServer)
			idJogador = isLocalPlayer ? 1 : 2;
		else
			idJogador = isLocalPlayer ? 2 : 1;

		PegarElementosCanvas();

		if (!isLocalPlayer)
			return;

		controladorNeuroSky = GameObject.Find("NeuroSkyTGCController").GetComponent<TGCConnectionController>();

		controladorNeuroSky.UpdatePoorSignalEvent += OnUpdateSinal;
		controladorNeuroSky.UpdateAttentionEvent += OnUpdateAtencao;

		if (isServer)
		{
			try
			{
				arduino = new SerialPort("COM7", 9600);
				arduino.ReadTimeout = 10;
				arduino.Open();

				StartCoroutine
				(
					LerArduino((string s) => CmdContabilizarVolta(s))
				);

				Debug.LogError("Iniciar arduíno.");
			}
			catch (IOException ioe) { }
		}
		else
		{
			InvokeRepeating("CmdSimularVoltas", 1f, 1.1f);
		}
	}

	[Command]
	void CmdSimularVoltas()
	{
		if (voltas >= controladorJogo.voltasTotal)
			return;

		voltas++;
		CmdContabilizarVolta(voltas.ToString());
	}

	void Update()
	{
		//if (!isServer || !controladorJogo.jogoIniciado) //  || !controladorJogo.corridaIniciada
		if (!controladorJogo.jogoIniciado) //  || !controladorJogo.corridaIniciada
			return;
		
		AtualizarJogador();
	}
	
	void AtualizarJogador()
	{
		if ((controladorJogo.corridaIniciada && controladorJogo.vencedorDeclarado && controladorJogo.vencedorCorrida != idJogador) ||
			(controladorJogo.corridaIniciada && !controladorJogo.vencedorDeclarado))
		{
			TimeSpan tempo = TimeSpan.FromSeconds(controladorJogo.tempoServidor - controladorJogo.tempoInicioCorrida);
			tempoText.text = string.Format("{0:00}:{1:00}:{2:00}", tempo.Minutes, tempo.Seconds, tempo.Milliseconds / 10);
		}

		if (tempoVoltas.Count > 0)
		{
			float melhorTempo = 0;
			foreach (float tempoVolta in tempoVoltas)
			{
				if (tempoVolta < melhorTempo || melhorTempo == 0)
					melhorTempo = tempoVolta;
			}

			TimeSpan tempo = TimeSpan.FromSeconds(melhorTempo);

			melhorTempoText.text = string.Format("{0:00}:{1:00}:{2:00}", tempo.Minutes, tempo.Seconds, tempo.Milliseconds / 10);
		}
		else
			melhorTempoText.text = "00:00:00";

		voltasText.text = voltas + "/" + controladorJogo.voltasTotal;
		concentracaoText.text = concentracao.ToString() + "%";
		sinalImage.sprite = controladorJogo.iconesSinal[indexIconeSinal];
	}

	[Command]
	void CmdContabilizarVolta(string s)
	{
		voltas = int.Parse(s);

		if (!controladorJogo.jogoIniciado)
			return;

		if (voltas > 0)
		{
			float tempoVolta = controladorJogo.tempoServidor - (voltas == 1 ? controladorJogo.tempoInicioCorrida : controladorJogo.tempoInicioCorrida + tempoVoltas[voltas - 2]);

			/*
			Debug.LogError("tempoServidor: " + controladorJogo.tempoServidor + " - tempoInicioCorrida: " + controladorJogo.tempoInicioCorrida);
			if (voltas > 1)
				Debug.LogError("tempoVoltas[voltas - 2]: " + tempoVoltas[voltas - 2]);
			*/

			tempoVoltas.Add(tempoVolta);
		}

		if (voltas == 0)
		{
			// Apenas durante os testes pois apenas um está no Arduíno
			Jogador jogador2 = controladorJogo.jogadores[1].GetComponent<Jogador>();
			jogador2.voltas = 0;
			jogador2.tempoVoltas.Clear();
			
			tempoVoltas.Clear();

			controladorJogo.IniciarCorrida();
		}
		else if (voltas == controladorJogo.voltasTotal)
		{
			if (controladorJogo.vencedorDeclarado)
			{
				controladorJogo.EncerrarCorrida();
			}
			else
			{
				controladorJogo.DeclararVencedor(idJogador);
			}
		}

		// Debug.LogError("Alterar voltas para " + voltas);
	}
	
	void OnUpdateSinal(int valor)
	{
		CmdAtualizarSinal(valor);
	}

	[Command]
	void CmdAtualizarSinal(int valor)
	{
		sinal = valor;
		if (valor < 25)
			indexIconeSinal = 0;
		else if (valor >= 25 && valor < 51)
			indexIconeSinal = 4;
		else if (valor >= 51 && valor < 78)
			indexIconeSinal = 3;
		else if (valor >= 78 && valor < 107)
			indexIconeSinal = 2;
		else if (valor >= 107)
			indexIconeSinal = 1;
	}
	
	void OnUpdateAtencao(int valor)
	{
		CmdAtualizarAtencao(valor);
	}

	[Command]
	void CmdAtualizarAtencao(int valor)
	{
		concentracao = valor;
	}

	public IEnumerator LerArduino(Action<string> callback, Action fail = null, float timeout = float.PositiveInfinity)
	{
		DateTime initialTime = DateTime.Now;
		DateTime nowTime;
		TimeSpan diff = default(TimeSpan);

		string dataString = null;

		do
		{
			try
			{
				dataString = arduino.ReadLine();
			}
			catch (TimeoutException)
			{
				dataString = null;
			}

			if (dataString != null)
			{
				callback(dataString);
				yield return null;
			}
			else
				yield return new WaitForSeconds(0.05f);

			nowTime = DateTime.Now;
			diff = nowTime - initialTime;

		} while (diff.Milliseconds < timeout);

		if (fail != null)
			fail();

		yield return null;
	}

	void OnApplicationQuit()
	{
		if (arduino != null)
			arduino.Close();
	}

	/*
	[SyncVar] internal int voltas;
	[SyncVar] internal int concentracao;
	[SyncVar] internal int indexIconeSinal = 1;
	[SyncVar] internal List<float> tempoVoltas = new List<float>();

	private ControladorJogo controladorJogo;
	private TGCConnectionController controladorNeuroSky;
	private SerialPort arduino;
	private int sinal;

	private Text voltasText;
	private Text tempoText;
	private Text concentracaoText;
	private Image sinalImage;

	void Start ()
	{
		controladorJogo = ControladorJogo.Pegar();

		controladorNeuroSky = GameObject.Find("NeuroSkyTGCController").GetComponent<TGCConnectionController>();

		controladorNeuroSky.UpdatePoorSignalEvent += OnUpdateSinal;
		controladorNeuroSky.UpdateAttentionEvent += OnUpdateAtencao;

		arduino = new SerialPort("COM7", 9600);
		arduino.ReadTimeout = 10;
		arduino.Open();

		StartCoroutine
		(
			LerArduino((string s) => ContabilizarVolta(s))
		);
	}

	void ContabilizarVolta(string s)
	{
		int voltaRecebida = int.Parse(s);
		voltas = voltaRecebida;

		if (voltaRecebida == 0)
			controladorJogo.IniciarCorrida();
		else
		{
			FinalizarVolta();

			if (voltaRecebida == controladorJogo.voltasTotal)
				FinalizarCorrida();
		}
	}

	void FinalizarCorrida()
	{
		if (!corridaIniciada)
			return;

		AtualizarJogadores();

		corridaIniciada = false;

		Debug.Log("FinalizarCorrida");
	}

	void FinalizarVolta()
	{
		tempoVoltas.Add(Time.time);

		Debug.Log("FinalizarVolta");
	}

	void OnUpdateSinal(int valor)
	{
		sinal = valor;
		if (valor < 25)
			indexIconeSinal = 0;
		else if (valor >= 25 && valor < 51)
			indexIconeSinal = 4;
		else if (valor >= 51 && valor < 78)
			indexIconeSinal = 3;
		else if (valor >= 78 && valor < 107)
			indexIconeSinal = 2;
		else if (valor >= 107)
			indexIconeSinal = 1;
	}

	void OnUpdateAtencao(int valor)
	{
		concentracao = valor;
	}

	public IEnumerator LerArduino(Action<string> callback, Action fail = null, float timeout = float.PositiveInfinity)
	{
		DateTime initialTime = DateTime.Now;
		DateTime nowTime;
		TimeSpan diff = default(TimeSpan);

		string dataString = null;

		do
		{
			try
			{
				dataString = arduino.ReadLine();
			}
			catch (TimeoutException)
			{
				dataString = null;
			}

			if (dataString != null)
			{
				callback(dataString);
				yield return null;
			}
			else
				yield return new WaitForSeconds(0.05f);

			nowTime = DateTime.Now;
			diff = nowTime - initialTime;

		} while (diff.Milliseconds < timeout);

		if (fail != null)
			fail();

		yield return null;
	}

	void OnApplicationQuit()
	{
		if (arduino != null)
			arduino.Close();
	}
	*/
}
