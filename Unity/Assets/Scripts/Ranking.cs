using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class Ranking : MonoBehaviour
{
	public int quantidadePontuacoes;
	public RectTransform canvasRanking;

	private string chaveBaseRanking = "Ranking";
	private string chaveBaseJogador = "_Jogador_";
	private string chaveBasePontuacao = "_Pontuação_";

	private List<string> nomesRanking;
	private List<float> pontuacoesRanking;
	private List<Text> canvasPosicaoJogadores = new List<Text>();
	private List<Text> canvasNomeJogadores = new List<Text>();
	private List<Text> canvasPontuacaoJogadores = new List<Text>();

	void Start ()
	{
		PegarCanvasRanking();

		AtualizarExibicaoRanking();
	}
	
	void Update ()
	{
		if (Input.GetKeyDown(KeyCode.I))
			AdicionarPontuacaoRanking("Teste " + UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 360));
		else if (Input.GetKeyDown(KeyCode.O))
			AtualizarExibicaoRanking();
		else if (Input.GetKeyDown(KeyCode.P))
			LimparRanking();
	}

	void PegarCanvasRanking()
	{
		for (int i = 0; i < quantidadePontuacoes; i++)
		{
			int posicaoRanking = i + 1;
			Transform canvasRankingPosicao = canvasRanking.FindChild("Pontuação" + posicaoRanking);

			Text canvasPosicaoJogador = canvasRankingPosicao.FindChild("Posição").GetComponent<Text>();
			Text canvasNomeJogador = canvasRankingPosicao.FindChild("Nome").GetComponent<Text>();
			Text canvasPontuacaoJogador = canvasRankingPosicao.FindChild("Pontuação").GetComponent<Text>();

			canvasPosicaoJogadores.Add(canvasNomeJogador);
			canvasNomeJogadores.Add(canvasNomeJogador);
			canvasPontuacaoJogadores.Add(canvasPontuacaoJogador);
		}
	}

	void AtualizarExibicaoRanking()
	{
		AtualizarValoresRanking();

		for (int i = 0; i < quantidadePontuacoes; i++)
		{
			int posicaoRanking = i + 1;
			string nomeRanking = nomesRanking[i];
			float pontuacaoRanking = pontuacoesRanking[i];

			canvasPosicaoJogadores[i].text = posicaoRanking.ToString();
			canvasNomeJogadores[i].text = nomeRanking;
			canvasPontuacaoJogadores[i].text = FormatarTempoCorrida(pontuacaoRanking);
		}
	}

	void AtualizarValoresRanking()
	{
		nomesRanking = new List<string>();
		pontuacoesRanking = new List<float>();

		for (int i = 0; i < quantidadePontuacoes; i++)
		{
			string chaveJogador = chaveBaseRanking + chaveBaseJogador + i;
			string chavePontuacao = chaveBaseRanking + chaveBasePontuacao + i;

			string nomeJogador = PlayerPrefs.GetString(chaveJogador);
			float pontuacao = PlayerPrefs.GetFloat(chavePontuacao);

			nomesRanking.Add(nomeJogador);
			pontuacoesRanking.Add(pontuacao);
		}
	}

	bool AdicionarPontuacaoRanking(string nomeJogador, float pontuacao)
	{
		AtualizarValoresRanking();
		
		for (int i = 0; i < quantidadePontuacoes; i++)
		{
			int posicaoRanking = i + 1;
			string nomeRanking = nomesRanking[i];
			float pontuacaoRanking = pontuacoesRanking[i];

			if (pontuacao > pontuacaoRanking)
			{
				string chaveJogador = chaveBaseRanking + chaveBaseJogador + i;
				string chavePontuacao = chaveBaseRanking + chaveBasePontuacao + i;

				PlayerPrefs.SetString(chaveJogador, nomeJogador);
				PlayerPrefs.SetFloat(chavePontuacao, pontuacao);

				if (posicaoRanking < quantidadePontuacoes)
					AdicionarPontuacaoRanking(nomeRanking, pontuacaoRanking);
				
				return true;
			}
		}

		return false;
	}

	void LimparRanking()
	{
		for (int i = 0; i < quantidadePontuacoes; i++)
		{
			string chaveJogador = chaveBaseRanking + chaveBaseJogador + i;
			string chavePontuacao = chaveBaseRanking + chaveBasePontuacao + i;

			PlayerPrefs.DeleteKey(chaveJogador);
			PlayerPrefs.DeleteKey(chavePontuacao);
		}
	}

	string FormatarTempoCorrida(float tempo)
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds(tempo);
		return string.Format("{0:00}'{1:00}\"{2:000}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
	}
}
