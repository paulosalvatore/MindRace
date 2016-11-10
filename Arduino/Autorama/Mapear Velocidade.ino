
int botaoInicio = A2;
int sensor = A4;

int pistaPositivo = 2;
int pista = 3;
int pistaNegativo = 4;
int energiaPista;

bool botaoInicioLiberado = true;
bool sensorLiberado = true;
unsigned long tempoSensor;
float delaySensor = 500;

unsigned long tempoInicio;

bool mapeamentoIniciado;
int valorMapeamento;
unsigned long mediaMapeamento;
int mapeamentoAutomaticoIntervalo[] = { 84, 50 };
bool mapeamentoAutomaticoFinalizado;

unsigned long tempoAtual;

int voltas = 0;
int voltasMax = 5;
int tempoVoltas[5];
int tempoVoltasExibicao[5];

void setup()
{
	pinMode(botaoInicio, INPUT);

	pinMode(sensor, INPUT);

	pinMode(pistaNegativo, OUTPUT);
	pinMode(pista, OUTPUT);
	pinMode(pistaPositivo, OUTPUT);

	Serial.begin(9600);
}

void IniciarMapeamento()
{
	voltas = 0;
	mapeamentoIniciado = true;
	tempoInicio = tempoAtual;

	digitalWrite(pistaPositivo, HIGH);
	digitalWrite(pistaNegativo, LOW);
	analogWrite(pista, valorMapeamento);
}

void EncerrarMapeamento()
{
	mapeamentoIniciado = false;

	long tempoVoltasTotal = 0;
	for (int i = 0; i < voltasMax; i++)
		tempoVoltasTotal += tempoVoltasExibicao[i];

	mediaMapeamento = tempoVoltasTotal / voltasMax;

	digitalWrite(pistaPositivo, LOW);
	digitalWrite(pistaNegativo, LOW);
	analogWrite(pista, 0);

	Serial.print("Mapeamento na velocidade ");
	Serial.print(valorMapeamento);
	Serial.print(" media ");
	Serial.print(mediaMapeamento);
	Serial.println(" milisegundos.");
}

void ContabilizarVolta()
{
	voltas++;

	long tempoVolta = tempoAtual - tempoInicio;
	long tempoVoltaExibicao = tempoAtual - (tempoInicio + (voltas > 1 ? tempoVoltas[voltas - 2] : 0));

	tempoVoltas[voltas - 1] = tempoVolta;
	tempoVoltasExibicao[voltas - 1] = tempoVoltaExibicao;

	/*
	Serial.print("Volta ");
	Serial.print(voltas);
	Serial.print(" - Mapeamento na velocidade ");
	Serial.print(valorMapeamento);
	Serial.print(" durou ");
	Serial.print(tempoVoltaExibicao);
	Serial.println(" milisegundos.");
	*/

	if (voltas == voltasMax)
		EncerrarMapeamento();
}

void ChecarBotaoInicio()
{
	bool leituraBotaoInicio = digitalRead(botaoInicio);
	if (botaoInicioLiberado && leituraBotaoInicio)
	{
		IniciarMapeamento();
		botaoInicioLiberado = false;
	}
	else if (!botaoInicioLiberado && !leituraBotaoInicio)
		botaoInicioLiberado = true;
}

void ChecarSensor()
{
	if (!mapeamentoIniciado)
		return;

	bool leituraSensor = digitalRead(sensor);
	if (sensorLiberado && leituraSensor)
	{
		sensorLiberado = false;
		ContabilizarVolta();
		tempoSensor = tempoAtual;
	}
	else if (!sensorLiberado && !leituraSensor && tempoAtual > tempoSensor + delaySensor) {
		sensorLiberado = true;
	}
}

void AtualizarValorMapeamento()
{
	if (Serial.available())
	{
		valorMapeamento = Serial.parseInt();
		Serial.print("Valor de mapeamento recebido: ");
		Serial.println(valorMapeamento);
	}
}

void ChecarMapeamentoAutomatico()
{
	if (mapeamentoAutomaticoFinalizado)
		return;

	if (!mapeamentoIniciado)
	{
		if (valorMapeamento == 0)
			valorMapeamento = mapeamentoAutomaticoIntervalo[0];
		else
			valorMapeamento--;

		if (valorMapeamento == mapeamentoAutomaticoIntervalo[1])
			mapeamentoAutomaticoFinalizado = true;

		IniciarMapeamento();
	}
}

void loop()
{
	tempoAtual = millis();

	AtualizarValorMapeamento();

	ChecarBotaoInicio();

	ChecarMapeamentoAutomatico();

	ChecarSensor();
}