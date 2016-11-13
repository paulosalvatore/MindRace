using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;

public class ControladorConexoes : MonoBehaviour
{
	public GameObject botaoConectarArduino;
	public Text portaArduinoText;
	public Dropdown listaPortasDropdown;

	private string[] nomesPortas;
	private Dictionary<string, SerialPort> portas = new Dictionary<string, SerialPort>();

	internal SerialPort arduino = null;

	void Start ()
	{
		AtualizarListaPortasDropdown();

		// IniciarLocalizacaoArduino();
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

	static public ControladorConexoes Pegar ()
	{
		return GameObject.Find("ControladorConexoes").GetComponent<ControladorConexoes>();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.R) && arduino == null)
		{
			try
			{
				SerialPort porta = new SerialPort("COM4", 9600);
				porta.ReadTimeout = 10;
				porta.Open();
				arduino = porta;
			}
			catch (IOException)
			{
				Debug.LogError("Erro IO ao apertar a letra R.");
			}
		}
	}
}
