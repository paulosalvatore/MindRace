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
- Caso nenhuma corrida esteja ativa, é possível iniciar o posicionamento automático dos carrinhos. Baseado em qual
pista está selecionada (externa ou interna), ao pressionar o botão de posicionamento, o carrinho irá até o sensor,
ficará parado por um pequeno período de tempo e iniciará o movimento novamente por um certo período de tempo até
que pare na posição desejada.
OBS.: Para ajustar a posição de parada, é necessário mexer na duração da energização da pista durante o posicionamento
através da variável array duracaoPosicionamentoAutomatico[] - Sendo que o primeiro valor é da posição externa e o
segundo é da posição interna. Também é possível mexer na velocidade do carrinho através da variável array
valorPosicionamentoAutomatico[] - seguindo as mesmas posições citadas acima para os valores de cada pista.
LEDs DE CONTROLE
Existem 8 LEDs que indicam o status de alguns componentes do circuito:
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
- LED Amarelo - Posicionamento Automático
Quando o botão de posicionamento automático for pressionado, o LED irá ficar aceso, caso contrário, ele ficará apagado.
- LED Laranja - Pista Externa Selecionada
Quando esse LED estiver aceso, significa que o arduino está programado para funcionar na pista externa, isso implicará
apenas no posicionamento automático, onde os valores de duração da energização são diferentes para cada pista.
- LED Amarelo - Pista Interna Selecionada
Quando esse LED estiver aceso, significa que o arduino está programado para funcionar na pista interna, isso implicará
apenas no posicionamento automático, onde os valores de duração da energização são diferentes para cada pista.

*/

#include <SimpleTimer.h>
#include <Adafruit_NeoPixel.h>

SimpleTimer simpleTimer;

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

int fitaLed = A5;

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
int maxVoltas = 6;
unsigned long tempoLedVoltas;
int contadorSensorVoltas;
int contadorNecessarioSensorVoltas = 5;

int concentracao = 0;
int concentracaoAleatoria = 0;
int concentracaoIntervalo[] = { 0, 100 };
unsigned long tempoUltimaConcentracaoRecebida;
float delayUltimaConcentracao = 10000;
bool ledConcentracaoLigado = false;
int variacaoConcentracaoAleatoria[] = { 10, 41 };

int energizarIntervalo[2][2] = {
	{62, 86},
	{60, 84}
};
int energizacaoFixa = 0;

unsigned long tempoAtual;
unsigned long tempoUltimaAtualizacao;
float delayAtualizacao = 2000;

bool botaoPosicionamentoLiberado = false;
bool posicionamentoLiberado = false;
bool posicionamentoIniciado = false;
int valorPosicionamentoAutomatico[] = { 69, 62 };
int duracaoPosicionamentoAutomatico[] = { 600, 0 };
int delayAposEtapa1 = 500;
unsigned long tempoEnergizacao;
float delayEnergizacao;

int pistaSelecionada;

int valoresRecebidos[3];
unsigned long tempoUltimoValorRecebido;
float delayUltimoValorRecebido = 100;
int quantidadeValoresRecebidos;

int totalLedsFita[] = { 9, 9 };
int coresLedsFita[2][3] = {
	{255, 0, 0},
	{0, 255, 0}
};

Adafruit_NeoPixel strip = Adafruit_NeoPixel(9, fitaLed, NEO_RGB + NEO_KHZ800);

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

	pinMode(fitaLed, OUTPUT);

	pinMode(ledPistaExterna, OUTPUT);
	pinMode(ledPistaInterna, OUTPUT);

	Serial.begin(9600);
	strip.begin();

	AtualizarLedsStatus();
	AtualizarLedPistaSelecionada();

	simpleTimer.setInterval(1000, AtualizarFitaLed);
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

void ApagarLedConcentracao()
{
	digitalWrite(ledConcentracao, LOW);
}

void AcenderLedConcentracao()
{
	digitalWrite(ledConcentracao, HIGH);
}

void PiscarLedConcentracao()
{
	ApagarLedConcentracao();
	simpleTimer.setTimeout(100, AcenderLedConcentracao);
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

void AtualizarFitaLed()
{
	for (int i = 0; i < totalLedsFita[pistaSelecionada]; i++)
	{
		int index = totalLedsFita[pistaSelecionada] - i;
		uint32_t cor = (concentracao == 0 || index > concentracao * totalLedsFita[pistaSelecionada] / 100 ? strip.Color(0, 0, 0) : strip.Color(coresLedsFita[pistaSelecionada][0], coresLedsFita[pistaSelecionada][1], coresLedsFita[pistaSelecionada][2]));

		strip.setPixelColor(i, cor);
	}

	strip.show();
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

	if (voltas < 0)
		voltas = 1;

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
	EnergizarPistaTempo(valorPosicionamentoAutomatico[pistaSelecionada], duracaoPosicionamentoAutomatico[pistaSelecionada]);
}

void ChecarSensor()
{
	if (!corridaIniciada && !posicionamentoIniciado)
		return;

	bool leituraSensor = !digitalRead(sensor);

	if (sensorLiberado && leituraSensor)
	{
		contadorSensorVoltas++;
		if (contadorSensorVoltas >= contadorNecessarioSensorVoltas)
		{
			sensorLiberado = false;
			AtualizarLedVoltas();
			if (corridaIniciada)
				ContarVoltas();
			else if (posicionamentoIniciado)
				ProcessarPosicionamento(2);
		}

		tempoSensor = tempoAtual;
	}
	else if (!sensorLiberado && !leituraSensor && tempoAtual > tempoSensor + delaySensor)
	{
		contadorSensorVoltas = 0;
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
			ApagarLedConcentracao();
			ledConcentracaoLigado = false;
		}
	}
	else if (tempoUltimaConcentracaoRecebida != 0 && tempoAtual < tempoUltimaConcentracaoRecebida + delayUltimaConcentracao && !ledConcentracaoLigado)
	{
		AcenderLedConcentracao();
		ledConcentracaoLigado = true;
	}
}

void ChecarRecebimentoSerial()
{
	if (Serial.available() > 0)
	{
		int dadoRecebido = Serial.read();

		if (dadoRecebido == 99)
			ChecarConexaoUnity(dadoRecebido);
		else
		{
			int valorRecebido = dadoRecebido - '0';
			tempoUltimoValorRecebido = tempoAtual;
			valoresRecebidos[quantidadeValoresRecebidos] = valorRecebido;
			quantidadeValoresRecebidos++;
		}
	}
}

void ValidarRecebimentoSerial()
{
	if (tempoAtual > tempoUltimoValorRecebido + delayUltimoValorRecebido && quantidadeValoresRecebidos > 0)
	{
		String valorRecebidoCompleto = "";
		for (int i = 0; i < quantidadeValoresRecebidos; i++)
		{
			int valor = valoresRecebidos[i];
			if (valor >= 0)
			{
				valorRecebidoCompleto += valoresRecebidos[i];
				valoresRecebidos[i] = -1;
			}
		}

		int valorRecebido = valorRecebidoCompleto.toInt();
		AtualizarConcentracao(valorRecebido);
		quantidadeValoresRecebidos = 0;
	}
}

void AtualizarConcentracao(int valor)
{
	concentracao = constrain(valor, 0, 100);
	PiscarLedConcentracao();
	tempoUltimaConcentracaoRecebida = tempoAtual;
}

void AtualizarConcentracaoAleatoria()
{
	if (tempoAtual > tempoUltimaAtualizacao + delayAtualizacao)
	{
		int modificador = random(variacaoConcentracaoAleatoria[0], variacaoConcentracaoAleatoria[1]);
		int multiplicador = concentracaoAleatoria > 30 ? random(0, 2) == 0 ? 1 : -1 : 1;

		concentracaoAleatoria = min(concentracaoIntervalo[1], max(concentracaoIntervalo[0], concentracao + modificador * multiplicador));

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

		int energizacaoIntervaloReal = energizarIntervalo[pistaSelecionada][1] - energizarIntervalo[pistaSelecionada][0];
		energia = corridaIniciada ? max(energizarIntervalo[pistaSelecionada][0], min(energizarIntervalo[pistaSelecionada][1], energizarIntervalo[pistaSelecionada][0] + concentracao * energizacaoIntervaloReal / 100)) : 0;
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
		energizacaoFixa = valorPosicionamentoAutomatico[pistaSelecionada];
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
		ApagarLedVoltas();
	}
}

void ChecarPosicionamentoAutomatico()
{
	if (corridaIniciada || posicionamentoIniciado)
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

void ChecarConexaoUnity(int dadoRecebido)
{
	// Serial.println(dadoRecebido);
}

void loop()
{
	simpleTimer.run();

	tempoAtual = millis();

	ChecarRecebimentoSerial();
	ValidarRecebimentoSerial();

	ChecarPosicionamentoAutomatico();

	ChecarBotaoInicio();
	ChecarEmergencia();
	ChecarSensor();
	ChecarPistaSelecionada();

	ValidarConcentracao();
	AtualizarConcentracaoAleatoria();

	EnergizarPista();
}
