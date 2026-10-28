// Volante de cartón: 1 potenciómetro (dirección) + 3 botones
// Envía por serial:  direccion,acelerar,retroceder,confirmar   Ej: 512,1,0,0

const int PIN_DIRECCION = A0;   // Pin central del potenciómetro (los extremos a 5V y GND)
const int PIN_ACELERAR  = 2;    // Botón entre el pin 2 y GND
const int PIN_RETROCEDER = 3;   // Botón entre el pin 3 y GND
const int PIN_CONFIRMAR = 4;    // Botón entre el pin 4 y GND

void setup() {
  Serial.begin(9600);
  pinMode(PIN_ACELERAR, INPUT_PULLUP);
  pinMode(PIN_RETROCEDER, INPUT_PULLUP);
  pinMode(PIN_CONFIRMAR, INPUT_PULLUP);
}

void loop() {
  int direccion = analogRead(PIN_DIRECCION);        // 0 a 1023
  int acelerar = !digitalRead(PIN_ACELERAR);        // con INPUT_PULLUP, presionado = LOW
  int retroceder = !digitalRead(PIN_RETROCEDER);
  int confirmar = !digitalRead(PIN_CONFIRMAR);

  Serial.print(direccion);  Serial.print(",");
  Serial.print(acelerar);   Serial.print(",");
  Serial.print(retroceder); Serial.print(",");
  Serial.println(confirmar);

  delay(30);
}
