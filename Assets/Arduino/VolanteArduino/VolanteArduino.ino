// Envía: acelerador,volante,freno,aceptar,pausa   Ej: 120,512,0,0,0

const int PIN_VOLANTE = A0;  // potenciómetro: extremos a 5V y GND, centro a A1
const int PIN_ACELERADOR  = A1;  // potenciómetro: extremos a 5V y GND, centro a A0
const int PIN_FRENO      = A2;  // potenciómetro: extremos a 5V y GND, centro a A2
const int PIN_ACEPTAR    = 4;   // botón entre pin 4 y GND
const int PIN_PAUSA      = 5;   // botón entre pin 5 y GND

void setup() {
  Serial.begin(9600);
  pinMode(PIN_ACEPTAR, INPUT_PULLUP);
  pinMode(PIN_PAUSA, INPUT_PULLUP);
}

void loop() {
  Serial.print(analogRead(PIN_VOLANTE)); Serial.print(",");
  Serial.print(analogRead(PIN_ACELERADOR));    Serial.print(",");
  Serial.print(analogRead(PIN_FRENO));      Serial.print(",");
  Serial.print(!digitalRead(PIN_ACEPTAR));  Serial.print(",");
  Serial.println(!digitalRead(PIN_PAUSA));

  delay(30);
}