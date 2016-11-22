using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using UnityEngine.SceneManagement;

public class ControladorConexoes : MonoBehaviour
{
	[Header("Arduino")]
	public GameObject arduinoCanvas;
	public Text portaArduinoText;
	public Dropdown listaPortasDropdown;

	[Header("MindWave")]
	public GameObject mindWaveCanvas;
	public Image sinalImage;

	[Header("Network Manager")]
	public NetworkManagerHUD networkManagerHUD;

	[Header("Ícones de Sinal")]
	public Sprite[] iconesSinal;
	private int sinal = 200;
	private int indexIconeSinal;

	[Header("Voltas")]
	public GameObject voltasCanvas;
	internal int voltasSelecionadas;
	private Text voltasSelecionadasText;

	private string[] nomesPortas;
	private Dictionary<string, SerialPort> portas = new Dictionary<string, SerialPort>();

	internal SerialPort arduino = null;
	private TGCConnectionController neuroskyControlador;

	void Start ()
	{
		neuroskyControlador = GameObject.Find("NeuroSkyTGCController").GetComponent<TGCConnectionController>();

		neuroskyControlador.UpdatePoorSignalEvent += OnUpdateSinal;
		
		AtualizarListaPortasDropdown();
		PegarVoltasSelecionadasText();

		// IniciarLocalizacaoArduino();
	}

	void PegarVoltasSelecionadasText()
	{
		voltasSelecionadasText = voltasCanvas.transform.FindChild("ValorSelecionado").GetComponent<Text>();
	}

	void Update()
	{
		if (arduinoCanvas && mindWaveCanvas)
			ChecarStatusConexoes();
	}

	void ChecarStatusConexoes()
	{
		if (sinal == 0)
		{
			AlterarExibicaoArduino(true);
			AlterarExibicaoVoltas(true);

			if (arduino != null)
				AlterarExibicaoNetworkHUD(true);
			else
				AlterarExibicaoNetworkHUD(false);
		}
		else
		{
			AlterarExibicaoArduino(false);
			AlterarExibicaoVoltas(false);
			AlterarExibicaoNetworkHUD(false);
		}
	}

	void OnUpdateSinal(int valor)
	{
		sinal = valor;
		indexIconeSinal = PegarIndexIconeSinal(valor);
		sinalImage.sprite = iconesSinal[indexIconeSinal];
	}

	void AlterarExibicaoArduino(bool exibicao)
	{
		if (arduinoCanvas.activeSelf != exibicao)
			arduinoCanvas.SetActive(exibicao);
	}

	void AlterarExibicaoVoltas(bool exibicao)
	{
		if (voltasCanvas.activeSelf != exibicao)
			voltasCanvas.SetActive(exibicao);
	}

	void AlterarExibicaoNetworkHUD(bool exibicao)
	{
		if (networkManagerHUD && networkManagerHUD.showGUI != exibicao)
			networkManagerHUD.showGUI = exibicao;
	}

	public void AtualizarListaPortasDropdown()
	{
		List<Dropdown.OptionData> listaPortas = new List<Dropdown.OptionData>();

		foreach (string nomePorta in SerialPort.GetPortNames())
			listaPortas.Add(new Dropdown.OptionData(nomePorta));

		listaPortasDropdown.ClearOptions();
		listaPortasDropdown.AddOptions(listaPortas);
	}

	public void ConectarPortaSelecionada()
	{
		try
		{
			string nomePortaSelecionada = listaPortasDropdown.options[listaPortasDropdown.value].text.ToString();
			arduino = new SerialPort(nomePortaSelecionada, 9600);
			arduino.ReadTimeout = 10;
			arduino.Open();
			if (arduino.IsOpen)
				portaArduinoText.text = nomePortaSelecionada;
		}
		catch (IOException) { }
	}

	/*
	public void IniciarLocalizacaoArduino()
	{
		StartCoroutine(LocalizarArduino());
	}

	IEnumerator LocalizarArduino()
	{
		nomesPortas = SerialPort.GetPortNames();
		
		foreach (string porta in nomesPortas)
		{
			ConectarPorta(porta);
			yield return new WaitForSeconds(0.1f);
		}
	}

	void ConectarPorta(string nomePorta)
	{
		SerialPort porta = new SerialPort(nomePorta, 9600);
		porta.ReadTimeout = 10;
		try
		{
			porta.Open();

			if (!portas.ContainsKey(nomePorta))
				portas.Add(nomePorta, porta);

			StartCoroutine(ChecarPorta(nomePorta, 3f));
		}
		catch (IOException ioe) { }
	}

	void DefinirPortaArduino(SerialPort porta)
	{
		arduino = porta;
		portaArduinoText.text = porta.PortName.ToString();
	}
	
	IEnumerator ChecarPorta(string nomePorta, float timeout)
	{
		float tempoInicial = Time.time;
		SerialPort porta = portas[nomePorta];

		while (true)
		{
			float tempoAtual = arduino == null ? Time.time - tempoInicial : timeout;

			if (tempoAtual > timeout)
			{
				FecharPorta(nomePorta);
				break;
			}

			try
			{
				porta.WriteLine("c");
				porta.BaseStream.Flush();
				string readLine = porta.ReadLine();
				DefinirPortaArduino(porta);
				break;
			}
			catch (TimeoutException) { }

			yield return new WaitForSeconds(0.05f);
		}
	}
	*/

	public int PegarIndexIconeSinal(int valor)
	{
		int indexIconeSinal;

		if (valor < 25)
			indexIconeSinal = 0;
		else if (valor >= 25 && valor < 51)
			indexIconeSinal = 4;
		else if (valor >= 51 && valor < 78)
			indexIconeSinal = 3;
		else if (valor >= 78 && valor < 107)
			indexIconeSinal = 2;
		else
			indexIconeSinal = 1;

		return indexIconeSinal;
	}

	public void AlterarVoltasSelecionadas()
	{
		InputField input = voltasCanvas.transform.FindChild("Input").GetComponent<InputField>();
		try
		{
			int valorInput = int.Parse(input.text);
			if (valorInput > 0)
			{
				voltasSelecionadas = valorInput;
				voltasSelecionadasText.text = voltasSelecionadas.ToString();
			}
		}
		catch (FormatException) { }
	}

	static public ControladorConexoes Pegar()
	{
		return GameObject.Find("ControladorConexoes").GetComponent<ControladorConexoes>();
	}

	void FecharPorta(string nomePorta)
	{
		SerialPort porta = portas[nomePorta];
		if (porta.IsOpen)
			porta.Close();
	}

	void OnApplicationQuit()
	{
		if (arduino != null && arduino.IsOpen)
			arduino.Close();

		foreach (KeyValuePair<string, SerialPort> porta in portas)
			FecharPorta(porta.Key);
	}
}
