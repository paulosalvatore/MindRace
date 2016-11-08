using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Diagnostics = System.Diagnostics;

public class Test : MonoBehaviour
{
    private SerialPort stream;
    private int voltas;

    void Start ()
	{
		/*foreach (string item in SerialPort.GetPortNames())
		{
			try
			{
				stream = new SerialPort(item, 9600);
				stream.ReadTimeout = 1;
				stream.Open();
				// Dá pra tentar dar um read, se der timeout é pq não é a porta do arduíno
				stream.Close();
			}
			catch (IOException) { }
		}*/
		
		/*
		// Parte funcionando - Lendo arduino e recebendo os dados pela porta serial
		stream = new SerialPort("COM7", 9600);
		stream.ReadTimeout = 10;
		stream.Open();
		
		StartCoroutine
		(
			AsynchronousReadFromArduino((string s) => Debug.Log(s))
		);
		*/

		//StartCoroutine(LerArduino());
		
	}

	/*IEnumerator LerArduino()
	{
		while (true)
		{
			float random = UnityEngine.Random.Range(0, 101);
			//Debug.Log(random);
			WriteToArduino(random);
			yield return new WaitForSeconds(1f);
			Debug.Log(ReadFromArduino(10));
			yield return new WaitForSeconds(1f);
		}
	}*/
	
	void Update ()
	{
		//Debug.Log("Voltas: " + voltas);
	}

	public void WriteToArduino(float numero)
	{
		stream.WriteLine(numero.ToString());
		stream.BaseStream.Flush();
	}

	public string ReadFromArduino(int timeout = 0)
	{
		stream.ReadTimeout = timeout;
		try
		{
			return stream.ReadLine();
		}
		catch (TimeoutException)
		{
			return null;
		}
	}

	public IEnumerator AsynchronousReadFromArduino(Action<string> callback, Action fail = null, float timeout = float.PositiveInfinity)
	{
		DateTime initialTime = DateTime.Now;
		DateTime nowTime;
		TimeSpan diff = default(TimeSpan);

		string dataString = null;

		do
		{
			try
			{
				dataString = stream.ReadLine();
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
		if (stream != null)
			stream.Close();
	}
}
