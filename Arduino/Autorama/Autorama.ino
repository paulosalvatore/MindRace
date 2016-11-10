/*

MindRace - Software do Arduino - Desenvolvido por Paulo Salvatore


VISÃO GERAL

- O programa inicia em um estado neutro, aguardando ação do botão de início.

- Ao acionar o botão de início, o programa irá iniciar uma corrida.

- A energização da pista se baseará nos valores que o programa recebe pela porta serial.

- Caso a comunicação entre o programa conectado ao MindWave e o arduino estiver comprometida, é possível acionar
o botão de emergência para que o programa comece a gerar valores aleatórios e energize a pista com base neles.

- Um outro sensor irá efetuar o trabalho de contagem das voltas, sempre que o sensor for acionado, o programa irá
contabilizar uma volta. Ao atingir o limite de voltas a corrida será finalizada e o programa irá retornar ao status
inicial.	Mesmo que o sensor continue ativo diversas vezes seguidamente, ele só irá aceitar um novo valor quando
entrar em estado inativo. Exemplo: Se o carrinho parar na frente do sensor, enviando constantemente uma informação,
o sensor irá considerar aquilo como apenas uma informação e só irá liberar uma nova entrada de informação quando o
carrinho sair da frente do sensor.

- O programa irá imprimir o número da volta atual na porta serial sempre que:
A corrida for iniciada;
O número da volta conforme o sensor de voltas for alterado;
A corrida for finalizada.


LEDs DE CONTROLE

Existem 5 LEDs que indicam o status de alguns componentes do circuito:

- LED Verde - Status - Corrida Ativa
Quando tiver uma corrida ativa, esse LED irá ficar aceso.
- LED Vermelho - Status - Nenhuma Corrida Ativa
Quando não tiver nenhuma corrida ativa, esse LED irá ficar aceso.
- LED Laranja - Concentração - Última Concentração Válida
Quando o programa receber um valor de concentração via porta serial, esse LED irá acender enquanto esse valor
estiver ativo, caso um novo valor não seja recebido em um certo prazo de segundos, esse LED irá apagar e o
valor de concentração será zerado.
- LED Amarelo - Emergência - Estado de Emergência acionado
Quando a chave de emergência for acionada esse LED irá ficar aceso para representar que o estado de emergência
está acionado na pista.
- LED Azul - Sensor - Estado do Sensor
Enquanto o sensor estiver obtendo algum valor esse LED irá ficar aceso. Assim que o sensor parar de emitir
sinal, o LED apagará. Enquanto o LED estiver ligado, não é possível contabilizar novas voltas na pista.

*/

#include <SimpleTimer.h>

SimpleTimer simpleTimer;

int botaoInicio = A2;
int chaveEmergencia = A1;
int chaveSelecaoPista = 13; // A0 - 13 temporário enquanto não ajeita os pinos
int botaoPosicionamento = A3;

int sensor = A4;

int pistaPositivo = 2;
int pista = 3;
int pistaNegativo = 4;
int energiaPista;

int ledOn = 12;
int ledOff = 11;
int ledConcentracao = 10;
int ledEmergencia = 9;
int ledVoltas = 8;
int ledPosicionamento = 7;
int ledPistaExterna = 6;
int ledPistaInterna = 5;

bool corridaIniciada = false;

bool emergenciaAtivado = false;

bool botaoInicioLiberado = true;
bool emergenciaLiberado = true;

bool sensorLiberado = false;
unsigned long tempoSensor;
float delaySensor = 500;

int voltas = 0;
int maxVoltas = 3;
unsigned long tempoLedVoltas;

int concentracao = 0;
int concentracaoAleatoria = 0;
int concentracaoMin = 0;
int concentracaoMax = 100;
unsigned long tempoUltimaConcentracaoRecebida;
float delayUltimaConcentracao = 5000;
bool ledConcentracaoLigado = false;
int variacaoConcentracaoAleatoria[] = {10, 41};

int energizarIntervalo[] = {50, 84};
int energizacaoFixa = 0;

unsigned long tempoAtual;
unsigned long tempoUltimaAtualizacao;
float delayAtualizacao = 2000;

bool botaoPosicionamentoLiberado = false;
bool posicionamentoLiberado = false;
bool posicionamentoIniciado = false;
int valorPosicionamentoAutomatico = 60;
int duracaoPosicionamentoAutomatico[] = {3150, 2150};
int delayAposEtapa1 = 500;
unsigned long tempoEnergizacao;
float delayEnergizacao;

int pistaSelecionada;

void AtualizarLedsStatus()
{
	digitalWrite(ledOn, corridaIniciada ? HIGH : LOW);
	digitalWrite(ledOff, corridaIniciada ? LOW : HIGH);
}

void setup()
{
	pinMode(botaoInicio, INPUT);
	pinMode(chaveEmergencia, INPUT);
	pinMode(chaveSelecaoPista, INPUT);
	pinMode(botaoPosicionamento, INPUT);

	pinMode(sensor, INPUT);

	pinMode(pistaNegativo, OUTPUT);
	pinMode(pista, OUTPUT);
	pinMode(pistaPositivo, OUTPUT);

	pinMode(ledOn, OUTPUT);
	pinMode(ledOff, OUTPUT);
	pinMode(ledConcentracao, OUTPUT);
	pinMode(ledEmergencia, OUTPUT);
	pinMode(ledVoltas, OUTPUT);
	pinMode(ledPosicionamento, OUTPUT);
	pinMode(ledPistaExterna, OUTPUT);
	pinMode(ledPistaInterna, OUTPUT);

	Serial.begin(9600);

	AtualizarLedsStatus();
}

void AtualizarLedEmergencia()
{
	digitalWrite(ledEmergencia, emergenciaAtivado ? HIGH : LOW);
}

void AtualizarLedVoltas(bool desligar = false)
{
	digitalWrite(ledVoltas, (!sensorLiberado && !desligar) ? HIGH : LOW);
}

void ApagarLedVoltas()
{
	AtualizarLedVoltas(true);
}

void PiscarLedConcentracao()
{
	ApagarLedConcentracao();
	simpleTimer.setTimeout(100, ApagarLedConcentracao);
}

void AcenderLedConcentracao()
{
	digitalWrite(ledConcentracao, HIGH);
}

void ApagarLedConcentracao()
{
	digitalWrite(ledConcentracao, LOW);
}

void AtualizarLedPosicionamento(bool desligar = false)
{
	digitalWrite(ledPosicionamento, posicionamentoIniciado ? HIGH : LOW);
}

void AtualizarLedPistaSelecionada()
{
	digitalWrite(ledPistaExterna, pistaSelecionada == 0 ? HIGH : LOW);
	digitalWrite(ledPistaInterna, pistaSelecionada == 1 ? HIGH : LOW);
}

void ExibirVoltas()
{
	Serial.println(voltas);
}

void IniciarCorrida()
{
	if (!corridaIniciada) {
		voltas = 0;
		corridaIniciada = true;
		ExibirVoltas();
		AtualizarLedsStatus();
	}
}

void EncerrarCorrida()
{
	corridaIniciada = false;
	AtualizarLedsStatus();
	simpleTimer.setTimeout(100, ApagarLedVoltas);
}

void ContarVoltas()
{
	voltas++;

	ExibirVoltas();

	if (voltas == maxVoltas)
		EncerrarCorrida();
}

void ChecarBotaoInicio()
{
	if (corridaIniciada || posicionamentoIniciado)
		return;

	bool leituraBotaoInicio = digitalRead(botaoInicio);
	if (botaoInicioLiberado && leituraBotaoInicio)
	{
		IniciarCorrida();
		botaoInicioLiberado = false;
	}
	else if (!botaoInicioLiberado && !leituraBotaoInicio)
		botaoInicioLiberado = true;
}

void ChecarEmergencia()
{
	bool leituraEmergencia = digitalRead(chaveEmergencia);
	if (leituraEmergencia && !emergenciaAtivado)
	{
		emergenciaAtivado = true;
		AtualizarLedEmergencia();
	}
	else if (!leituraEmergencia && emergenciaAtivado)
	{
		emergenciaAtivado = false;
		AtualizarLedEmergencia();
		concentracao = 0;
	}
}

void ChecarPistaSelecionada()
{
	bool leituraSelecaoPista = digitalRead(chaveSelecaoPista);
	if (leituraSelecaoPista != pistaSelecionada)
	{
		pistaSelecionada = leituraSelecaoPista;
		AtualizarLedPistaSelecionada();
	}
}

void EnergizarPistaTempo(int energia, float tempo)
{
	energizacaoFixa = energia;
	delayEnergizacao = tempo;
	tempoEnergizacao = tempoAtual;
	EnergizarPista();
}

void EnergizarPistaTempoAutomatico()
{
	EnergizarPistaTempo(valorPosicionamentoAutomatico, duracaoPosicionamentoAutomatico[pistaSelecionada]);
}

void ChecarSensor()
{
	if (!corridaIniciada && !posicionamentoIniciado)
		return;

	bool leituraSensor = digitalRead(sensor);

	if (sensorLiberado && leituraSensor)
	{
		sensorLiberado = false;
		AtualizarLedVoltas();
		if (corridaIniciada)
			ContarVoltas();
		else if (posicionamentoIniciado)
			ProcessarPosicionamento(2);

		tempoSensor = tempoAtual;
	}
	else if (!sensorLiberado && !leituraSensor && tempoAtual > tempoSensor + delaySensor) {
		sensorLiberado = true;
		AtualizarLedVoltas();
	}
}

void ValidarConcentracao()
{
	if (tempoAtual > tempoUltimaConcentracaoRecebida + delayUltimaConcentracao)
	{
		if (!emergenciaAtivado)
			concentracao = 0;

		if (ledConcentracaoLigado)
		{
			digitalWrite(ledConcentracao, LOW);
			ledConcentracaoLigado = false;
		}
	}
	else if (tempoUltimaConcentracaoRecebida != 0 && tempoAtual < tempoUltimaConcentracaoRecebida + delayUltimaConcentracao && !ledConcentracaoLigado)
	{
		digitalWrite(ledConcentracao, HIGH);
		ledConcentracaoLigado = true;
	}
}

void AtualizarConcentracao()
{
	if (Serial.available())
	{
		concentracao = Serial.parseInt();
		PiscarLedConcentracao();
		tempoUltimaConcentracaoRecebida = tempoAtual;
	}
}

void AtualizarConcentracaoAleatoria()
{
	if (tempoAtual > tempoUltimaAtualizacao + delayAtualizacao)
	{
		int modificador = random(variacaoConcentracaoAleatoria[0], variacaoConcentracaoAleatoria[1]);
		int multiplicador = concentracaoAleatoria > 30 ? random(0, 2) == 0 ? 1 : -1 : 1;

		concentracaoAleatoria = min(concentracaoMax, max(concentracaoMin, concentracao + modificador * multiplicador));

		tempoUltimaAtualizacao = tempoAtual;
	}
}

void EnergizarPista()
{
	int energia;
	if (energizacaoFixa > 0)
		energia = energizacaoFixa;
	else if (energizacaoFixa == -1)
	{
		energia = 0;
		energizacaoFixa = 0;
	}
	else
	{
		if (emergenciaAtivado)
			concentracao = concentracaoAleatoria;

		energia = (corridaIniciada) ? max(energizarIntervalo[0], concentracao * energizarIntervalo[1] / 100) : 0;
	}

	if (tempoEnergizacao > 0 && tempoAtual > tempoEnergizacao + delayEnergizacao)
	{
		delayEnergizacao = 0;
		tempoEnergizacao = 0;
		energizacaoFixa = 0;
	}

	if (energia != energiaPista)
	{
		digitalWrite(pistaPositivo, HIGH);
		digitalWrite(pistaNegativo, LOW);
		analogWrite(pista, energia);
		energiaPista = energia;
	}
}

void DesenergizarPista()
{
	energizacaoFixa = -1;
	EnergizarPista();
}

void IniciarPosicionamento()
{
	if (!posicionamentoIniciado)
	{
		ProcessarPosicionamento(1);
		posicionamentoIniciado = true;
		AtualizarLedPosicionamento();
	}
}

void ProcessarPosicionamento(int etapaPosicionamento)
{
	if (etapaPosicionamento == 1)
		energizacaoFixa = valorPosicionamentoAutomatico;
	else if (etapaPosicionamento == 2)
	{
		DesenergizarPista();
		simpleTimer.setTimeout(delayAposEtapa1, EnergizarPistaTempoAutomatico);
		simpleTimer.setTimeout(duracaoPosicionamentoAutomatico[pistaSelecionada], EncerrarPosicionamento);
	}
}

void EncerrarPosicionamento()
{
	if (posicionamentoIniciado)
	{
		posicionamentoIniciado = false;
		AtualizarLedPosicionamento();
	}
}

void ChecarPosicionamentoAutomatico()
{
	if (corridaIniciada)
		return;

	bool leituraBotaoPosicionamento = digitalRead(botaoPosicionamento);
	if (botaoPosicionamentoLiberado && leituraBotaoPosicionamento)
	{
		IniciarPosicionamento();
		botaoPosicionamentoLiberado = false;
	}
	else if (!botaoPosicionamentoLiberado && !leituraBotaoPosicionamento)
		botaoPosicionamentoLiberado = true;
}

void loop()
{
	tempoAtual = millis();

	ChecarPosicionamentoAutomatico();

	ChecarBotaoInicio();
	ChecarEmergencia();
	ChecarSensor();
	ChecarPistaSelecionada();

	ValidarConcentracao();
	AtualizarConcentracao();
	AtualizarConcentracaoAleatoria();

	EnergizarPista();

	delay(1); // Apenas para controlar o tempo de execução do programa
}
