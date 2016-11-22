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
	private ControladorConexoes controladorConexoes;
	private TGCConnectionController controladorNeuroSky;
	private SerialPort arduino;

	[SyncVar] internal int voltas;
	[SyncVar] internal int concentracao;
	[SyncVar] internal int indexIconeSinal = 1;
	[SyncVar] internal SyncListInt concentracaoLista = new SyncListInt();

	[SyncVar] internal SyncListFloat tempoVoltas = new SyncListFloat();

	[SyncVar] internal int concentracaoMedia;
	[SyncVar] internal int concentracaoMenor;
	[SyncVar] internal int concentracaoMaior;
	[SyncVar] internal bool exibindoEstatisticas = false;
	[SyncVar] internal bool estatisticasLiberadas = true;

	internal int idJogador;

	private Transform canvasJogadorCorrida;
	private Vector2 posicaoInicialCanvasCorrida;
	private Text voltasAtualText;
	private Text melhorTempoText;
	private Text tempoText;
	private Image arcoPreenchimentoImage;
	private Text concentracaoText;
	private Image sinalImage;
	private Image premioImage;

	private Transform canvasJogadorEstatisticas;
	private Vector2 posicaoInicialCanvasEstatisticas;
	private Text estatisticasTempoText;
	private Text estatisticasMelhorVoltaText;
	private Text estatisticasConcentracaoMenorText;
	private Image estatisticasConcentracaoMenorImage;
	private Text estatisticasConcentracaoMaiorText;
	private Image estatisticasConcentracaoMaiorImage;
	private Text estatisticasConcentracaoMediaText;
	private Image estatisticasConcentracaoMediaImage;

	void Start()
	{
		controladorJogo = ControladorJogo.Pegar();
		controladorConexoes = ControladorConexoes.Pegar();

		if (isServer)
			idJogador = isLocalPlayer ? 1 : 2;
		else
			idJogador = isLocalPlayer ? 2 : 1;

		PegarElementosCanvas();

		if (!isLocalPlayer)
			return;

		controladorNeuroSky = GameObject.Find("NeuroSkyTGCController").GetComponent<TGCConnectionController>();

		controladorNeuroSky.UpdatePoorSignalEvent += OnUpdateSinal;
		controladorNeuroSky.UpdateAttentionEvent += OnUpdateConcentracao;

		if (controladorConexoes.arduino != null)
		{
			arduino = controladorConexoes.arduino;
			StartCoroutine
			(
				LerArduino((string s) => CmdContabilizarVolta(s))
				// LerArduino((string s) => Debug.Log(s))
			);
		}

		StartCoroutine(GravarArduino());
	}

	void Update()
	{
		if (isServer && isLocalPlayer)
		{
			if (Input.GetKeyDown(KeyCode.Q))
				CmdContabilizarVolta("0");
			else if (Input.GetKeyDown(KeyCode.W))
				CmdContabilizarVolta("1");
			else if (Input.GetKeyDown(KeyCode.E))
				CmdContabilizarVolta("2");
			else if (Input.GetKeyDown(KeyCode.R))
				CmdContabilizarVolta("3");
		}
		else if (!isServer && isLocalPlayer)
		{
			if (Input.GetKeyDown(KeyCode.A))
				CmdContabilizarVolta("0");
			else if (Input.GetKeyDown(KeyCode.S))
				CmdContabilizarVolta("1");
			else if (Input.GetKeyDown(KeyCode.D))
				CmdContabilizarVolta("2");
			else if (Input.GetKeyDown(KeyCode.F))
				CmdContabilizarVolta("3");
		}

		if (Input.GetKeyDown(KeyCode.Z))
			ExibirEstatisticas(false);
		else if (Input.GetKeyDown(KeyCode.X))
			OcultarEstatisticas(false);

		AtualizarConcentracaoJogador();

		if (!controladorJogo.jogoIniciado)
			return;

		AtualizarJogador();

		ChecarExibirEstatisticas();
	}

	void ChecarExibirEstatisticas()
	{
		if (!estatisticasLiberadas || exibindoEstatisticas || controladorJogo.corridaIniciada || (!controladorJogo.corridaIniciada && !controladorJogo.vencedorDeclarado))
			return;

		if (controladorJogo.tempoServidor - controladorJogo.tempoFinalCorrida > controladorJogo.delayExibirEstatisticas)
			ExibirEstatisticas();
	}

	void PegarElementosCanvas()
	{
		int indexJogador = idJogador - 1;

		GameObject canvasJogador = controladorJogo.canvasJogadores[indexJogador];
		canvasJogadorCorrida = canvasJogador.transform.FindChild("Corrida");
		RectTransform transformCanvasCorrida = canvasJogadorCorrida.GetComponent<RectTransform>();
		posicaoInicialCanvasCorrida = transformCanvasCorrida.anchoredPosition;

		voltasAtualText = canvasJogadorCorrida.FindChild("VoltasAtual").GetComponent<Text>();
		melhorTempoText = canvasJogadorCorrida.FindChild("MelhorTempo").GetComponent<Text>();
		tempoText = canvasJogadorCorrida.FindChild("Tempo").GetComponent<Text>();
		arcoPreenchimentoImage = canvasJogadorCorrida.FindChild("ArcoPreenchimento").GetComponent<Image>();
		concentracaoText = canvasJogadorCorrida.FindChild("Concentração").GetComponent<Text>();
		sinalImage = canvasJogadorCorrida.FindChild("Sinal").GetComponent<Image>();
		premioImage = controladorJogo.premioImageJogadores[indexJogador];

		canvasJogadorEstatisticas = canvasJogador.transform.FindChild("Estatísticas");
		RectTransform transformCanvasEstatisticas = canvasJogadorEstatisticas.GetComponent<RectTransform>();
		posicaoInicialCanvasEstatisticas = transformCanvasEstatisticas.anchoredPosition;

		estatisticasTempoText = canvasJogadorEstatisticas.FindChild("TempoTotal").FindChild("Valor").GetComponent<Text>();
		estatisticasMelhorVoltaText = canvasJogadorEstatisticas.FindChild("MelhorVolta").FindChild("Valor").GetComponent<Text>();

		Transform estatisticasCanvasConcentracao = canvasJogadorEstatisticas.FindChild("Concentração");
		estatisticasConcentracaoMenorText = estatisticasCanvasConcentracao.FindChild("Menor").FindChild("Valor").GetComponent<Text>();
		estatisticasConcentracaoMenorImage = estatisticasCanvasConcentracao.FindChild("Menor").FindChild("ArcoPreenchimento").GetComponent<Image>();
		estatisticasConcentracaoMaiorText = estatisticasCanvasConcentracao.FindChild("Maior").FindChild("Valor").GetComponent<Text>();
		estatisticasConcentracaoMaiorImage = estatisticasCanvasConcentracao.FindChild("Maior").FindChild("ArcoPreenchimento").GetComponent<Image>();
		estatisticasConcentracaoMediaText = estatisticasCanvasConcentracao.FindChild("Média").FindChild("Valor").GetComponent<Text>();
		estatisticasConcentracaoMediaImage = estatisticasCanvasConcentracao.FindChild("Média").FindChild("ArcoPreenchimento").GetComponent<Image>();
	}

	Vector2 PegarAnchoredPosition(GameObject elemento)
	{
		return elemento.GetComponent<RectTransform>().anchoredPosition;
	}

	void MoverCanvasJogadorCorrida(Vector2 posicao, float duracao)
	{
		Vector2 anchoredPosition = PegarAnchoredPosition(canvasJogadorCorrida.gameObject);
		iTween.ValueTo(canvasJogadorCorrida.gameObject, iTween.Hash(
			"from", anchoredPosition,
			"to", posicao,
			"easetype", iTween.EaseType.easeInOutSine,
			"time", duracao,
			"onupdatetarget", this.gameObject,
			"onupdate", "MoverCanvasElementoCorrida")
		);
	}

	void MoverCanvasElementoCorrida(Vector2 posicao)
	{
		canvasJogadorCorrida.gameObject.GetComponent<RectTransform>().anchoredPosition = posicao;
	}

	void MoverCanvasJogadorEstatisticas(Vector2 posicao, float duracao)
	{
		Vector2 anchoredPosition = PegarAnchoredPosition(canvasJogadorEstatisticas.gameObject);
		iTween.ValueTo(canvasJogadorEstatisticas.gameObject, iTween.Hash(
			"from", anchoredPosition,
			"to", posicao,
			"easetype", iTween.EaseType.easeInOutSine,
			"time", duracao,
			"onupdatetarget", this.gameObject,
			"onupdate", "MoverCanvasElementoEstatisticas")
		);
	}

	void MoverCanvasElementoEstatisticas(Vector2 posicao)
	{
		canvasJogadorEstatisticas.gameObject.GetComponent<RectTransform>().anchoredPosition = posicao;
	}

	void AtualizarConcentracaoJogador()
	{
		float quantidade = arcoPreenchimentoImage.fillAmount;
		arcoPreenchimentoImage.fillAmount = quantidade = Mathf.Lerp(quantidade, concentracao / 100f, Time.deltaTime);

		concentracaoText.text = string.Format("{0}%", (int)(quantidade * 100));
		sinalImage.sprite = controladorConexoes.iconesSinal[indexIconeSinal];
	}

	string FormatarTempoCorrida(float tempo)
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds(tempo);
		return string.Format("{0:00}'{1:00}\"{2:000}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
	}

	void AtualizarJogador()
	{
		if ((controladorJogo.corridaIniciada && controladorJogo.vencedorDeclarado && controladorJogo.vencedorCorrida != idJogador) ||
			(controladorJogo.corridaIniciada && !controladorJogo.vencedorDeclarado))
			tempoText.text = FormatarTempoCorrida(controladorJogo.tempoServidor - controladorJogo.tempoInicioCorrida);

		if (tempoVoltas.Count > 0)
			melhorTempoText.text = FormatarTempoCorrida(PegarMelhorTempo());
		else
			melhorTempoText.text = FormatarTempoCorrida(0);

		if (!controladorJogo.corridaIniciada && controladorJogo.vencedorDeclarado && estatisticasLiberadas)
		{
			AlterarExibicaoPremio(true);
			premioImage.sprite = controladorJogo.trofeus[controladorJogo.vencedorCorrida == idJogador ? 0 : 1];
		}
		else
			AlterarExibicaoPremio(false);

		voltasAtualText.text = string.Format("{0:00}", voltas);
	}

	float PegarMelhorTempo()
	{
		float melhorTempo = 0;
		for (int i = 0; i < tempoVoltas.Count; i++)
		{
			float tempoVolta = tempoVoltas[i];
			float tempoVoltaReal = tempoVolta - (i == 0 ? 0 : tempoVoltas[i - 1]);
			if (tempoVoltaReal < melhorTempo || melhorTempo == 0)
				melhorTempo = tempoVoltaReal;
		}
		return melhorTempo;
	}

	void AlterarExibicaoPremio(bool exibicao)
	{
		if (premioImage.gameObject.activeSelf != exibicao)
			premioImage.gameObject.SetActive(exibicao);
	}

	public void PrepararInicioCorrida()
	{
		tempoVoltas.Clear();
		concentracaoLista.Clear();
		tempoText.text = FormatarTempoCorrida(0);
		melhorTempoText.text = FormatarTempoCorrida(0);
		AlterarExibicaoPremio(false);
		voltas = 0;
		controladorJogo.AlterarExibicaoVencedor(false);
	}

	[Command]
	void CmdContabilizarVolta(string s)
	{
		try
		{
			voltas = int.Parse(s);

			if (!controladorJogo.jogoIniciado || voltas < 0)
				return;

			if (voltas > 0)
			{
				float tempoVolta = controladorJogo.tempoServidor - controladorJogo.tempoInicioCorrida;

				if (tempoVoltas.Count < voltas)
					tempoVoltas.Add(tempoVolta);
				else
					tempoVoltas[voltas] = tempoVolta;
			}

			if (voltas == 0)
				controladorJogo.IniciarCorrida();
			else if (voltas == controladorJogo.voltasTotal)
			{
				if (controladorJogo.vencedorDeclarado)
				{
					AtualizarJogador();
					controladorJogo.EncerrarCorrida();
				}
				else
					controladorJogo.DeclararVencedor(idJogador);
			}
		}
		catch (FormatException) { }
	}

	void OnUpdateSinal(int valor)
	{
		CmdAtualizarSinal(valor);
	}

	[Command]
	void CmdAtualizarSinal(int valor)
	{
		indexIconeSinal = controladorConexoes.PegarIndexIconeSinal(valor);
	}

	void OnUpdateConcentracao(int valor)
	{
		CmdAtualizarConcentracao(valor);
	}

	[Command]
	void CmdAtualizarConcentracao(int valor)
	{
		concentracao = valor;

		if (controladorJogo.corridaIniciada && (!controladorJogo.vencedorDeclarado || (controladorJogo.vencedorDeclarado && controladorJogo.vencedorCorrida != idJogador)))
			concentracaoLista.Add(concentracao);
	}

	public void CalcularEstatisticasConcentracao()
	{
		int concentracaoTotal = 0;
		concentracaoMedia = 0;
		concentracaoMenor = -1;
		concentracaoMaior = -1;

		foreach (int concentracao in concentracaoLista)
		{
			concentracaoTotal += concentracao;

			if (concentracaoMenor == -1 || concentracao < concentracaoMenor)
				concentracaoMenor = concentracao;

			if (concentracaoMaior == -1 || concentracao > concentracaoMaior)
				concentracaoMaior = concentracao;
		}

		if (concentracaoLista.Count > 0)
			concentracaoMedia = concentracaoTotal / concentracaoLista.Count;
	}

	void ExibirEstatisticas(bool inserirDados = true)
	{
		if (exibindoEstatisticas)
			return;

		Vector2 posicao = posicaoInicialCanvasEstatisticas;
		float duracao = controladorJogo.duracaoAnimacaoCanvas;
		MoverCanvasJogadorCorrida(posicao, duracao);
		Invoke("ExibirCanvasEstatisticas", duracao);
		exibindoEstatisticas = true;
		estatisticasLiberadas = false;

		if (inserirDados)
			Invoke("InserirDadosEstatisticas", duracao * 3);
	}

	void InserirDadosEstatisticas()
	{
		Invoke("InserirTempoTotal", controladorJogo.duracaoAnimacaoTempo);
	}

	void InserirTempoTotal()
	{
		if (tempoVoltas.Count == controladorJogo.voltasTotal)
		{
			float tempoTotal = tempoVoltas[controladorJogo.voltasTotal - 1];
			// estatisticasTempoText.text = FormatarTempoCorrida(tempoTotal);
			StartCoroutine(InserirTempo(tempoTotal, estatisticasTempoText));
		}

		Invoke("InserirMelhorVolta", controladorJogo.duracaoAnimacaoTempo);
	}

	void InserirMelhorVolta()
	{
		float melhorVolta = PegarMelhorTempo();
		// estatisticasMelhorVoltaText.text = FormatarTempoCorrida(melhorVolta);
		StartCoroutine(InserirTempo(melhorVolta, estatisticasMelhorVoltaText));
		Invoke("InserirConcentracaoMenor", controladorJogo.duracaoAnimacaoConcentracao);
	}

	IEnumerator InserirTempo(float valor, Text text)
	{
		float valorInserido = 0f;

		while (valorInserido != valor && exibindoEstatisticas)
		{
			valorInserido = Mathf.Lerp(valorInserido, valor, 0.1f);
			text.text = FormatarTempoCorrida(valorInserido);
			yield return null;
		}
	}

	void InserirConcentracaoMenor()
	{
		StartCoroutine(InserirConcentracao(concentracaoMenor, estatisticasConcentracaoMenorImage, estatisticasConcentracaoMenorText));
		Invoke("InserirConcentracaoMaior", controladorJogo.duracaoAnimacaoConcentracao);
	}

	void InserirConcentracaoMaior()
	{
		StartCoroutine(InserirConcentracao(concentracaoMaior, estatisticasConcentracaoMaiorImage, estatisticasConcentracaoMaiorText));
		Invoke("InserirConcentracaoMedia", controladorJogo.duracaoAnimacaoConcentracao);
	}

	void InserirConcentracaoMedia()
	{
		StartCoroutine(InserirConcentracao(concentracaoMedia, estatisticasConcentracaoMediaImage, estatisticasConcentracaoMediaText));
		Invoke("OcultarEstatisticas", controladorJogo.delayOcultarEstatisticas);
	}

	IEnumerator InserirConcentracao(float valor, Image image, Text text)
	{
		float quantidade = image.fillAmount;

		while (quantidade != valor && exibindoEstatisticas)
		{
			image.fillAmount = quantidade = Mathf.Lerp(quantidade, valor / 100f, 0.01f);

			text.text = string.Format("{0}%", (int)(quantidade * 100));
			yield return null;
		}
	}

	public void OcultarEstatisticas()
	{
		OcultarEstatisticas(true);
	}

	void OcultarEstatisticas(bool prepararInicioCorrida)
	{
		if (!exibindoEstatisticas)
			return;

		Vector2 posicao = posicaoInicialCanvasEstatisticas;
		float duracao = controladorJogo.duracaoAnimacaoCanvas;
		exibindoEstatisticas = false;
		MoverCanvasJogadorEstatisticas(posicao, duracao);
		Invoke("ExibirCanvasCorrida", duracao);

		if (prepararInicioCorrida)
			PrepararInicioCorrida();
	}

	void ExibirCanvasCorrida()
	{
		Vector2 posicao = posicaoInicialCanvasCorrida;
		float duracao = controladorJogo.duracaoAnimacaoCanvas;
		MoverCanvasJogadorCorrida(posicao, duracao);
	}

	void ExibirCanvasEstatisticas()
	{
		Vector2 posicao = posicaoInicialCanvasCorrida;
		float duracao = controladorJogo.duracaoAnimacaoCanvas;
		MoverCanvasJogadorEstatisticas(posicao, duracao);
	}

	public void ZerarEstatisticas()
	{
		estatisticasTempoText.text = FormatarTempoCorrida(0);
		estatisticasMelhorVoltaText.text = FormatarTempoCorrida(0);
		estatisticasConcentracaoMenorText.text = "0%";
		estatisticasConcentracaoMenorImage.fillAmount = 0;
		estatisticasConcentracaoMaiorText.text = "0%";
		estatisticasConcentracaoMaiorImage.fillAmount = 0;
		estatisticasConcentracaoMediaText.text = "0%";
		estatisticasConcentracaoMediaImage.fillAmount = 0;
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
				// Debug.LogError(dataString);
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

	IEnumerator GravarArduino()
	{
		while (true)
		{
			if (arduino != null)
			{
				string gravarValor = indexIconeSinal == 0 ? concentracao.ToString() : "0";
				arduino.Write(gravarValor);
				arduino.BaseStream.Flush();
			}
			yield return new WaitForSeconds(1f);
		}
	}

	void OnApplicationQuit()
	{
		if (arduino != null)
			arduino.Close();
	}
}
