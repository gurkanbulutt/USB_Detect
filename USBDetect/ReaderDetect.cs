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
    public partial class ReaderDetect : Form
    {

        private const int WM_DEVICECHANGE = 0x219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_VOLUME = 0x00000002;
        private const int DbtDevtypDeviceinterface = 5;
        private static readonly Guid GuidDevinterfaceUSBDevice = new Guid("50DD5230-BA8A-11D1-BF5D-0000F805F530"); // USB devices
        private static IntPtr notificationHandle;

        public const int SCARD_S_SUCCESS = 0;

        public static class SmartCardScope
        {
            public static readonly Int32 User = 0;
            public static readonly Int32 Terminal = 1;
            public static readonly Int32 System = 2;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SmartCardReaderState
        {
            public string cardReaderString;
            public IntPtr userDataPointer;
            public UInt32 currentState;
            public UInt32 eventState;
            public UInt32 atrLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            public byte[] ATR;
        }

        public static class SmartCardState
        {
            public static readonly UInt32 Unaware = 0x00000000;
            public static readonly UInt32 Ignore = 0x00000001;
            public static readonly UInt32 Changed = 0x00000002;
            public static readonly UInt32 Unknown = 0x00000004;
            public static readonly UInt32 Unavailable = 0x00000008;
            public static readonly UInt32 Empty = 0x00000010;
            public static readonly UInt32 Present = 0x00000020;
            public static readonly UInt32 Atrmatch = 0x00000040;
            public static readonly UInt32 Exclusive = 0x00000080;
            public static readonly UInt32 Inuse = 0x00000100;
            public static readonly UInt32 Mute = 0x00000200;
            public static readonly UInt32 Unpowered = 0x00000400;
        }

        private static List<string> ParseReaderBuffer(byte[] buffer)
        {
            var str = Encoding.ASCII.GetString(buffer);
            if (string.IsNullOrEmpty(str)) return new List<string>();
            return new List<string>(str.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private static bool CheckIfFlagsSet(UInt32 mask, params UInt32[] flagList)
        {
            foreach (UInt32 flag in flagList)
            {
                if (IsFlagSet(mask, flag)) return true;
            }

            return false;
        }

        private static bool IsFlagSet(UInt32 mask, UInt32 flag)
        {
            return ((flag & mask) > 0);
        }

        [DllImport("winscard.dll")]
        internal static extern int SCardEstablishContext(Int32 dwScope, IntPtr pReserved1, IntPtr pReserved2, out Int32 hContext);

        [DllImport("winscard.dll", EntryPoint = "SCardListReadersA", CharSet = CharSet.Ansi)]
        internal static extern int SCardListReaders(Int32 hContext, byte[] cardReaderGroups, byte[] readersBuffer, out UInt32 readersBufferLength);

        [DllImport("winscard.dll")]
        internal static extern int SCardGetStatusChange(Int32 hContext, UInt32 timeoutMilliseconds, [In, Out] SmartCardReaderState[] readerStates, Int32 readerCount);


        public ReaderDetect()
        {
            InitializeComponent();
        }

        private void ReaderDetect_Load(object sender, EventArgs e)
        {

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

                            int context = 0;

                            Console.WriteLine("Checking card readers...");
                            var result = SCardEstablishContext(SmartCardScope.User, IntPtr.Zero, IntPtr.Zero, out context);
                            if (result != SCARD_S_SUCCESS) throw new Exception("Smart card error: " + result.ToString());

                            uint bufferLength = 10000;
                            byte[] readerBuffer = new byte[bufferLength];

                            result = SCardListReaders(context, null, readerBuffer, out bufferLength);
                            if (result != SCARD_S_SUCCESS) throw new Exception("Smart card error: " + result.ToString());

                            var readers = ParseReaderBuffer(readerBuffer);

                            Console.WriteLine("{0} Card Reader(s)", readers.Count);
                            if (readers.Any())
                            {
                                var readerStates = readers.Select(cardReaderName => new SmartCardReaderState() { cardReaderString = cardReaderName }).ToArray();

                                result = SCardGetStatusChange(context, 1000, readerStates, readerStates.Length);
                                if (result != SCARD_S_SUCCESS) throw new Exception("Smart card error: " + result.ToString());


                                readerStates.ToList().ForEach(readerState => Console.WriteLine("Reader: {0}, State: {1}", readerState.cardReaderString,
                                    CheckIfFlagsSet(readerState.eventState, SmartCardState.Present, SmartCardState.Atrmatch) ? "Card Present" : "Card Absent"));
                            }

                            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity");

                            foreach (ManagementObject queryObj in searcher.Get())
                            {

                                if (queryObj["Description"].Equals("ACR1281 1S Dual Reader ICC"))
                                {
                                    Console.WriteLine("Correct SmartCardReader inserted.");
                                    break;
                                }
                            }
                            listBox1.Items.Add("Cihaz takıldı.");
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
