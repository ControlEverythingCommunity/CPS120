// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace CPS120
{
    struct PressTemp
    {
        public double P;
        public double C;
        public double F;
    }; /// <summary>
       /// App that reads data over I2C from an CPS120, Pressure and Temperature Sensor
       /// </summary>

    public sealed partial class MainPage : Page
    {
        private const byte PRESTEMP_I2C_ADDR = 0x28;           // I2C address of the CPS120
	private const byte PRESTEMP_REG_PRESSURE = 0x00;       // Pressure data register


        private I2cDevice I2CPrestemp;
        private Timer periodicTimer;

        public MainPage()
        {
            this.InitializeComponent();

            // Register for the unloaded event so we can clean up upon exit
            Unloaded += MainPage_Unloaded;

            // Initialize the I2C bus, Pressure and Temperature Sensor, and timer
            InitI2CPrestemp();
        }

        private async void InitI2CPrestemp()
        {
            string aqs = I2cDevice.GetDeviceSelector();             // Get a selector string that will return all I2C controllers on the system
            var dis = await DeviceInformation.FindAllAsync(aqs);    // Find the I2C bus controller device with our selector string
            if (dis.Count == 0)
            {
                Text_Status.Text = "No I2C controllers were found on the system";
                return;
            }

            var settings = new I2cConnectionSettings(PRESTEMP_I2C_ADDR);
            settings.BusSpeed = I2cBusSpeed.FastMode;
            I2CPrestemp = await I2cDevice.FromIdAsync(dis[0].Id, settings);    // Create an I2C Device with our selected bus controller and I2C settings
            if (I2CPrestemp == null)
            {
                Text_Status.Text = string.Format(
                    "Slave address {0} on I2C Controller {1} is currently in use by " +
                    "another application. Please ensure that no other applications are using I2C.",
                    settings.SlaveAddress,
                    dis[0].Id);
                return;
            }

            /*
		Initialize the Pressure and Temperature Sensor
		For this device, we create 1-byte write buffer
		The byte is the content that we want to write to
	    */
            byte[] WriteBuf_StartByte = new byte[] { 0x80 };          // 0x80 Send start command

            // Write the register settings
            try
            {
                I2CPrestemp.Write(WriteBuf_StartByte);
            }
            // If the write fails display the error and stop running
            catch (Exception ex)
            {
                Text_Status.Text = "Failed to communicate with device: " + ex.Message;
                return;
            }

            // Create a timer to read data every 100ms
            periodicTimer = new Timer(this.TimerCallback, null, 0, 100);
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            // Cleanup
            I2CPrestemp.Dispose();
        }

        private void TimerCallback(object state)
        {
            string pText, cText, fText;
            string addressText, statusText;

            // Read and format Pressure and Temperature data
            try
            {
                PressTemp PRESTEMP = ReadI2CPrestemp();
                addressText = "I2C Address of the Pressure and Temperature Sensor CPS120: 0x28";
                pText = String.Format("Digital Pressure (kPa): {0:F2}", PRESTEMP.P);
                cText = String.Format("Temperature in Celsius (°C): {0:F2}", PRESTEMP.C);
                fText = String.Format("Temperature in Fahrenheit (°F): {0:F2}", PRESTEMP.F);
                statusText = "Status: Running";
            }
            catch (Exception ex)
            {
                pText = "Digital Pressure: Error";
                cText = "Temperature in Celsius: Error";
                fText = "Temperature in Fahrenheit: Error";
                statusText = "Failed to read from Pressure and Temperature Sensor: " + ex.Message;
            }

            // UI updates must be invoked on the UI thread
            var task = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Text_Digital_Pressure.Text = pText;
                Text_Temperature_in_Celsius.Text = cText;
                Text_Temperature_in_Fahrenheit.Text = fText;
                Text_Status.Text = statusText;
            });
        }

        private PressTemp ReadI2CPrestemp()
        {
            byte[] RegAddrBuf = new byte[] { PRESTEMP_REG_PRESSURE };   // Read data from the register address
            byte[] ReadBuf = new byte[4];                       	// We read 4 bytes sequentially to get all 2 two-byte pressure and temperature registers in one read

            /*
		Read from the Pressure and Temperature Sensor 
		We call WriteRead() so we first write the address of the Digital Pressure I2C register, then read Temperature data register
	    */
            I2CPrestemp.WriteRead(RegAddrBuf, ReadBuf);

	    // In order to get the raw 16-bit data values, we need to concatenate two 8-bit bytes from the I2C read.

            ushort PRESTEMPRawP = (ushort)((ReadBuf[0] & 0x3F) * 256);
            PRESTEMPRawP |= (ushort)(ReadBuf[1] & 0xFF);
            ushort PRESTEMPRawC = (ushort)((ReadBuf[2] & 0xFF) * 256);
            PRESTEMPRawP |= (ushort)(ReadBuf[3] & 0xFC);

            // Conversions using formulas provided
            double pressure = ((PRESTEMPRawP / 16384.0) * 90.0) + 30.0;
            double ctemp = (((PRESTEMPRawC / 4.0) /16384.0) * 165.0) - 40.0;
            double ftemp = (ctemp * 1.8) + 32.0;

            PressTemp prstmp;
            prstmp.P = pressure;
            prstmp.C = ctemp;
            prstmp.F = ftemp;

            return prstmp;
        }
    }
}
