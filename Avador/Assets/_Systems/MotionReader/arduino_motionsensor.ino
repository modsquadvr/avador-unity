const int sensorPin = 7;
int sensorState = 0;

void setup() {
  Serial.begin(9600);
  pinMode(sensorPin, INPUT);
}

void loop() {
  sensorState = digitalRead(sensorPin);

  if(sensorState == HIGH){
    Serial.write('1');
  }
  else {
    Serial.write('0');
  }

  delay(500);
}
