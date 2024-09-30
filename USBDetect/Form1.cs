using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Management;
using Microsoft.Win32;
using System.IO.Ports;

namespace USBDetect
{
    public partial class Form1 : Form
    {
        private const int WM_DEVICECHANGE = 0x219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_VOLUME = 0x00000002;
        public Form1()
        {
            InitializeComponent();
        }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            switch (m.Msg)
            {
                case WM_DEVICECHANGE:
                    switch ((int)m.WParam)
                    {
                        case DBT_DEVICEARRIVAL:
                            string[] portnames=SerialPort.GetPortNames();
                            Console.WriteLine(portnames[0]);
                            listBox1.Items.Add(portnames[0] + "takıldı.");

                            break;

                        case DBT_DEVICEREMOVECOMPLETE:
                            listBox1.Items.Add("Cihaz çıkarıldı.");
                            break;
                    }
                    break;
            }
        }
    }
}
