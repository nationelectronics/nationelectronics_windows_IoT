using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Devices.Enumeration;


namespace MCP3208ADC
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            /* Register for the unloaded event so we can clean up upon exit */
            Unloaded += MainPage_Unloaded;

            /* Initialize SPI and Timer*/
            InitAll();
        }

        /* Initialize SPI and Timer*/
        private async void InitAll()
        {
            try
            {
                await InitSPI();    /* Initialize the SPI bus for communicating with the ADC      */
            }
            catch (Exception ex)
            {
                StatusText.Text = ex.Message;
                return;
            }

            /* Now that everything is initialized, create a timer so we read data every 500mS */
            periodicTimer = new Timer(this.Timer_Tick, null, 0, 100);

            StatusText.Text = "Status: Running";
        }

        private async Task InitSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT);
                settings.ClockFrequency = 500000;   /* 0.5MHz clock rate                                        */
                settings.Mode = SpiMode.Mode0;      /* The ADC expects idle-low clock polarity so we use Mode0  */

                var controller = await SpiController.GetDefaultAsync();
                SpiADC = controller.GetDevice(settings);
            }

            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
        }


        /* Read from the ADC, update the UI */
        private void Timer_Tick(object state)
        {
            int[] adcValue = new int[8];
            double[] volts = new double[8];
            byte i;

            for (i = 0; i < 8; i++)
            {
                adcValue[i] = ReadADC(i);
                volts[i] = adcValue[i] * Vref / 4095;
            }

            /* UI updates must be invoked on the UI thread */
            var task = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                /* Display the value on screen*/
                textPlaceHolder0.Text = "Volt0=" + volts[0].ToString("F4") + "V ADC0=" + adcValue[0].ToString();
                textPlaceHolder1.Text = "Volt1=" + volts[1].ToString("F4") + "V ADC1=" + adcValue[1].ToString();        
                textPlaceHolder2.Text = "Volt2=" + volts[2].ToString("F4") + "V ADC2=" + adcValue[2].ToString();
                textPlaceHolder3.Text = "Volt3=" + volts[3].ToString("F4") + "V ADC3=" + adcValue[3].ToString();
                textPlaceHolder4.Text = "Volt4=" + volts[4].ToString("F4") + "V ADC4=" + adcValue[4].ToString();
                textPlaceHolder5.Text = "Volt5=" + volts[5].ToString("F4") + "V ADC5=" + adcValue[5].ToString();
                textPlaceHolder6.Text = "Volt6=" + volts[6].ToString("F4") + "V ADC6=" + adcValue[6].ToString();
                textPlaceHolder7.Text = "Volt7=" + volts[7].ToString("F4") + "V ADC7=" + adcValue[7].ToString(); 
            });
        }

        public int ReadADC(byte channel)
        {
            byte[] readBuffer = new byte[3]; /* Buffer to hold read data*/
            byte[] writeBuffer = new byte[3] { 0x00, 0x00, 0x00 };

            /* Setup the appropriate ADC configuration byte */
            writeBuffer[0] = (byte)(6 + ((channel & 4) >> 2));
            writeBuffer[1] = (byte)((channel & 3) << 6);
            writeBuffer[2] = 0;

            SpiADC.TransferFullDuplex(writeBuffer, readBuffer);  /* Read data from the ADC                           */
            return ((readBuffer[1] & 15) << 8) + readBuffer[2];  /* Convert the returned bytes into an integer value */
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            /* It's good practice to clean up after we're done */
            if (SpiADC != null)
            {
                SpiADC.Dispose();
            }

        }
        private SpiDevice SpiADC;
        private Timer periodicTimer;

        private const Int32 SPI_CHIP_SELECT = 0;  /* (jumper CE0 on) SPI_CHIP_SELECT = 0 (default), (jumper CE1 on) SPI_CHIP_SELECT = 1  */
        private const double Vref = 5.0;          /* jumper selected: 5.0 (default), 3.3, 1.0, or 0.3 Volts                              */

    }
}

