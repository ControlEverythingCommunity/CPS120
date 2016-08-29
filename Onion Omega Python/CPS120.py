# Distributed with a free-will license.
# Use it any way you want, profit or free, provided it fits in the licenses of its associated works.
# CPS120
# This code is designed to work with the CPS120_I2CS I2C Mini Module available from ControlEverything.com.
# https://www.controleverything.com/content/Barometer?sku=CPS120_I2CS#tabs-0-product_tabset-2

from OmegaExpansion import onionI2C
import time

# Get I2C bus
i2c = onionI2C.OnionI2C()

# CPS120 address, 0x28(40)
#		0x80(128)	 Select Start mode
bytes   = [0x80]
i2c.write(0x28, bytes)

time.sleep(0.1)

# CPS120 address, 0x28(40)
# Read data back from 0x00(0), 4 bytes
# pressure MSB, pressure LSB, cTemp MSB, cTemp LSB
data = i2c.readBytes(0x28, 0x00, 4)

# Convert the data to kPa
pressure = (((data[0] & 0x3F) * 256 + data[1]) / 16384.0) * 90 + 30
cTemp = ((((data[2] * 256) + (data[3] & 0xFC)) / 4.0 ) * (165.0 / 16384.0)) - 40.0
fTemp = cTemp * 1.8 + 32

# Output data to screen
print "Pressure is : %.2f kPa" %pressure
print "Temperature in Celsius : %.2f C" %cTemp
print "Temperature in Fahrenheit : %.2f C" %fTemp
