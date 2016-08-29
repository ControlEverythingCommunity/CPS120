// Distributed with a free-will license.
// Use it any way you want, profit or free, provided it fits in the licenses of its associated works.
// CPS120
// This code is designed to work with the CPS120_I2CS I2C Mini Module available from ControlEverything.com.
// https://www.controleverything.com/content/Barometer?sku=CPS120_I2CS#tabs-0-product_tabset-2

#include <ESP8266WiFi.h>
#include <WiFiClient.h>
#include <ESP8266WebServer.h>
#include <Wire.h>

// CPS120 I2C address is 0x28(40)
#define Addr 0x28

const char* ssid = "Dcube_web";
const char* password = "12345670";
float pressure, cTemp, fTemp;

ESP8266WebServer server(80);

void handleroot()
{
  unsigned int data[4];

  // Start I2C Transmission
  Wire.beginTransmission(Addr);

  // Request 4 byte of data
  Wire.requestFrom(Addr, 4);

  // Read 4 bytes of data
  // pressure msb, pressure lsb, temp msb, temp lsb
  if (Wire.available() == 4)
  {
    data[0] = Wire.read();
    data[1] = Wire.read();
    data[2] = Wire.read();
    data[3] = Wire.read();
    delay(300);

    // Stop I2C Transmission
    Wire.endTransmission();

    // Convert the data to 14 bits
    pressure = ((((data[0] & 0x3F) * 265 + data[1]) / 16384.0 ) * 90.0 ) + 30.0 ;
    cTemp = ((((data[2] * 256) + (data[3] & 0xFC)) / 4.0 ) * (165.0 / 16384.0)) - 40.0;
    fTemp = cTemp * 1.8 + 32;

    // Output data to serial monitor
    Serial.print("Pressure is :   ");
    Serial.print(pressure);
    Serial.println(" kPa");
    Serial.print("Temperature in Celsius : ");
    Serial.print(cTemp);
    Serial.println(" C");
    Serial.print("Temperature in Fahrenheit : ");
    Serial.print(fTemp);
    Serial.println(" F");

    // Output data to web server
    server.sendContent
    ("<html><head><meta http-equiv='refresh' content='5'</meta>"
     "<h1 style=text-align:center;font-size:300%;color:blue;font-family:britannic bold;>CONTROL EVERYTHING</h1>"
     "<h3 style=text-align:center;font-family:courier new;><a href=http://www.controleverything.com/ target=_blank>www.controleverything.com</a></h3><hr>"
     "<h2 style=text-align:center;font-family:tahoma;><a href=https://www.controleverything.com/content/Barometer?sku=CPS120_I2CS#tabs-0-product_tabset-2/ \n"
     "target=_blank>CPS120 Sensor I2C Mini Module</a></h2>");
    server.sendContent
    ("<h3 style=text-align:center;font-family:tahoma;>Pressure = " + String(pressure) + " kPa");
    server.sendContent
    ("<h3 style=text-align:center;font-family:tahoma;>Temperature in Celsius = " + String(cTemp) + " C");
    server.sendContent
    ("<h3 style=text-align:center;font-family:tahoma;>Temperature in Fahrenheit = " + String(fTemp) + " F");
  }
  delay(1000);
}

void setup()
{
  // Initialise I2C communication as MASTER
  Wire.begin(2, 14);
  // Initialise serial communication, set baud rate = 115200
  Serial.begin(115200);

  // Connect to WiFi network
  WiFi.begin(ssid, password);

  // Wait for connection
  while (WiFi.status() != WL_CONNECTED)
  {
    delay(500);
    Serial.print(".");
  }
  Serial.println("");
  Serial.print("Connected to ");
  Serial.println(ssid);

  // Get the IP address of ESP8266
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());

  // Start the server
  server.on("/", handleroot);
  server.begin();
  Serial.println("HTTP server started");
}

void loop()
{
  server.handleClient();
}
