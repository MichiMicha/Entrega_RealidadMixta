// Volante de cartón. Formato de los profesores (ArduinoSerial.cs + ArduinoController.cs):
//   "acelerador,timon,freno,ciclo,atras\n"
//   - Potenciómetros: 0-1023
//   - Botones: 1 = presionado, 0 = suelto
//
// En ESTE montaje hay solo 2 potenciómetros (acelerador y timón). Para tener también FRENO,
// el potenciómetro del acelerador funciona como PALANCA:
//        FRENO  <----  [zona neutra]  ---->  ACELERADOR
// El Arduino hace la separación y manda "acelerador" y "freno" ya listos, así que en Unity
// no hay que cambiar nada del código de los profesores.
//
// Botones (cada uno del pin a GND, sin resistencia):
//   CICLO (pin 2): toque corto = siguiente botón del menú / cambiar vista; mantener = seleccionar
//   ATRÁS (pin 3): volver / abrir el menú de pausa

// ---------- Pines ----------
const int PIN_ACELERADOR = A0;   // Potenciómetro de la palanca (extremos a 5V y GND)
const int PIN_TIMON = A1;        // Potenciómetro del timón
const int PIN_CICLO = 2;
const int PIN_ATRAS = 3;

// ---------- Calibración de la palanca ----------
const bool PALANCA = true;               // true: un lado acelera y el otro frena. false: potenciómetro normal (solo acelera)
const int NEUTRO = 512;                  // Valor crudo con la palanca en el centro (míralo en el Monitor Serie)
const int ZONA_NEUTRA = 40;              // Margen alrededor del neutro que cuenta como "nada"
const bool ACELERAR_AL_AUMENTAR = true;  // false si acelerar queda del lado de los números BAJOS

void setup() {
  Serial.begin(9600);
  pinMode(PIN_CICLO, INPUT_PULLUP);   // con PULLUP el pin lee LOW cuando se presiona
  pinMode(PIN_ATRAS, INPUT_PULLUP);
}

void loop() {
  int lectura = analogRead(PIN_ACELERADOR);
  int acelerador = 0;
  int freno = 0;

  if (PALANCA) {
    // d > 0: palanca hacia el lado de acelerar. d < 0: hacia el lado de frenar.
    int d = ACELERAR_AL_AUMENTAR ? (lectura - NEUTRO) : (NEUTRO - lectura);
    int recorridoAcelerar = ACELERAR_AL_AUMENTAR ? (1023 - NEUTRO) : NEUTRO;
    int recorridoFrenar = ACELERAR_AL_AUMENTAR ? NEUTRO : (1023 - NEUTRO);

    if (d > ZONA_NEUTRA) {
      acelerador = map(d, ZONA_NEUTRA, recorridoAcelerar, 0, 1023);
    } else if (d < -ZONA_NEUTRA) {
      freno = map(-d, ZONA_NEUTRA, recorridoFrenar, 0, 1023);
    }
    acelerador = constrain(acelerador, 0, 1023);
    freno = constrain(freno, 0, 1023);
  } else {
    acelerador = lectura;   // modo normal: sin freno (se manda 0 en esa posición)
  }

  Serial.print(acelerador);                              // index 0
  Serial.print(",");
  Serial.print(analogRead(PIN_TIMON));                   // index 1
  Serial.print(",");
  Serial.print(freno);                                   // index 2
  Serial.print(",");
  Serial.print(digitalRead(PIN_CICLO) == LOW ? 1 : 0);   // index 3
  Serial.print(",");
  Serial.println(digitalRead(PIN_ATRAS) == LOW ? 1 : 0); // index 4

  delay(20); // ~50 Hz
}
