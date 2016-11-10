/*

MindRace - Software do Arduino - Desenvolvido por Paulo Salvatore


Versão atualizada compatível com o Circuits.IO - Usada apenas em testes onlines.

Essa versão não utiliza a library 'SimpleTimer'.

*/


int botaoInicio = A2;
int chaveEmergencia = A1;
int chaveSelecaoPista = A0;
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
	AtualizarLedPistaSelecionada();
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
	delay(100);
	ApagarLedConcentracao();
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
	delay(100);
	ApagarLedVoltas();
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
		ChecarSensor();

		delay(delayAposEtapa1);

		EnergizarPistaTempoAutomatico();
		ChecarSensor();

		delay(duracaoPosicionamentoAutomatico[pistaSelecionada]);

		EncerrarPosicionamento();
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
