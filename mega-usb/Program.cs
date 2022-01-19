using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace mega_usb
{
    class Program
    {

        static void Main(string[] args)
        {

            SerialPort port;

            string option = null;

            try
            {

                System.Console.WriteLine("");
                System.Console.WriteLine("============================");
                System.Console.WriteLine("Mega EverDrive usb-tool-plus");
                System.Console.WriteLine("============================");
                System.Console.WriteLine("");

                if ((args.Length == 1) && ((args[0].Contains("?") || args[0].Contains("-help"))))
                {
                    System.Console.WriteLine("mega-usb {filename.ext} [-options]");
                    System.Console.WriteLine("");
                    System.Console.WriteLine("Fast usage by fixed \"filename.ext\" (w/o options):");
                    System.Console.WriteLine("");
                    System.Console.WriteLine("  MEGA.BIN        - Load OS image to Mega EverDrive v2(x7), x3 ,x5");
                    System.Console.WriteLine("MEGAOS.bin        - Load OS image to Mega EverDrive v1");
                    System.Console.WriteLine("     *.RBF        - Load FPGA config");
                    System.Console.WriteLine("     *.SMS        - Load as MasterSystem ROM");
                    System.Console.WriteLine("       *.*        - Load as MegaDrive (SMD) Generic ROM (up to 4MB)");
                    System.Console.WriteLine("");
                    System.Console.WriteLine("Options detail usage for any \"filename.ext\" (excl. see above):");
                    System.Console.WriteLine("");
                    System.Console.WriteLine("         -o       - Load file as OS image to Mega EverDrive v1");
                    System.Console.WriteLine("                    (Equivalent to MEGAOS.bin without options)");
                    System.Console.WriteLine("         -fo      - Load file as Firmware image to Mega EverDrive v1");
                    System.Console.WriteLine("                    (Equivalent to MEGAOS.bin without options)");
                    System.Console.WriteLine("         -sms     - Load file as MasterSystem ROM");
                    System.Console.WriteLine("         -os      - Load file as EverDrive Application");
                    System.Console.WriteLine("         -cd      - Load file as Mega-CD addon ROM");
                    System.Console.WriteLine("         -32x     - Load file as 32X addon ROM");
                    System.Console.WriteLine("         -m10     - Load file as SMD \"big-ROM\" (10MB w/o mapper)");
                    System.Console.WriteLine("         -ssf     - Load file as SMD ROM with SSF-mapper");
                    System.Console.WriteLine("         -smd     - Load file as SMD Generic ROM (default)");
                    System.Console.WriteLine("");
                    System.Console.WriteLine("Example (w/o SD-card, the sequence of actions is as follows):");
                    System.Console.WriteLine("");
                    System.Console.WriteLine("mega-usb SVP.RBF  - Load FPGA config");
                    System.Console.WriteLine("mega-usb MEGA.BIN - Load OS image");
                    System.Console.WriteLine("mega-usb ROM.BIN  - Load SMD ROM");
                    System.Console.WriteLine("");
                    System.Console.WriteLine("* Before next ROM load - press Reset key on EverDrive or SMD");
                    System.Console.WriteLine("(?) OpenSource build 2021");
                    return; // Exit form this tool ...
                }

                System.Console.WriteLine("Testing link ...");
                port = searchCart();
                System.Console.WriteLine("-> Mega EverDrive detected on " + port.PortName);

                if (args.Length == 0)
                {
                    System.Console.WriteLine("");
                    System.Console.WriteLine("For take detail about use - run with /? or -help");
                    return; // Exit form this tool ...
                }

                FileStream f = File.OpenRead(args[0]);
                int len = (int)f.Length;
                if (len % 65536 != 0) len = len / 65536 * 65536 + 65536;
                byte[] buff = new byte[len];
                f.Read(buff, 0, (int)f.Length);
                f.Close();

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Contains("MEGA.BIN") || args[i].Contains(".RBF") || args[i].Contains(".rbf"))
                    {
                        System.Console.WriteLine("Selected Mega EverDrive v2 hardware init");
                        v2_sysTools(args, port);
                        port.Close();
                        return; // Exit form this tool ...
                    }
                }

                if (args.Length == 1)
                {
                    option = args[0].Contains("MEGAOS.bin") ? "-o" : args[0].Contains(".sms") | args[0].Contains(".SMS") ? "-sms" : "-smd";
                    if (buff[261] == 'S' && buff[262] == 'S' && buff[263] == 'F' && buff[264] == ' ') option = "-ssf";
                    else if (len > 4096 * 1024) option = "-m10";
                }
                else if (args.Length == 2)
                {
                    option = args[1];
                }
                else throw new Exception("... too many command-line argument");

                if (option.Equals("-o") || option.Equals("-fo"))
                {
                    System.Console.WriteLine("Selected Mega EverDrive v1 hardware init");
                    loadOS(option, buff, port);
                }
                else
                {
                    loadGame(option, buff, port);
                }

            }
            catch (Exception x)
            {
                System.Console.WriteLine("\nERROR: " + x.Message);
            }

        }


        static void loadOS(string arg, byte[] data, SerialPort port)
        {
            if (data.Length > 0x100000) throw new Exception("OS file is too big: wrong file?");
            port.ReadTimeout = 1000;
            port.WriteTimeout = 1000;
            byte[] tx = new byte[1];

            if (arg.Equals("-fo"))
            {
                System.Console.WriteLine("Firmware loading...");
                tx[0] = (byte)(64 + 32);
                port.Write("*f");
                port.Write(tx, 0, 1);
                if (port.ReadByte() != 'k') throw new Exception("... unexpected response");

                sendData(data, port, 0x8400, 0x18000); // Send Firmware

                port.ReadTimeout = 200;
                port.WriteTimeout = 200;
                Thread.Sleep(1000);

                for (int i = 0; ; i++)
                {
                    if (i > 10) throw new Exception("... OS reloading timeout");

                    try
                    {
                        for (;;) port.ReadByte();
                    }
                    catch (Exception) { }

                    try
                    {
                        port.Write("*T");
                        if (port.ReadByte() != 'k') throw new Exception("... unexpected response");
                        break;
                    }
                    catch (Exception) { }
                }

                port.ReadTimeout = 1000;
                port.WriteTimeout = 1000;
                System.Console.WriteLine("-> Done!");

            }

            System.Console.WriteLine("OS loading ...");
            tx[0] = (byte)(data.Length / 512 / 128);
            port.Write("*o");
            port.Write(tx, 0, 1);
            if (port.ReadByte() != 'k') throw new Exception("... unexpected response");

            sendData(data, port, 0, data.Length); // Send OS

            System.Console.WriteLine("-> Done!");

        }


        static void loadGame(string arg, byte[] data, SerialPort port)
        {
            byte[] tx = new byte[1];
            System.Console.WriteLine("Game loading init ...");
            port.ReadTimeout = 1000;
            port.WriteTimeout = 1000;
            port.Write("*g");
            tx[0] = (byte)(data.Length / 512 / 128);

            port.Write(tx, 0, 1);
            System.Console.WriteLine("-> Size: " + data.Length);

            if (port.ReadByte() != 'k') throw new Exception("... unexpected response: link disconnected?");
            if (data.Length > 0xf00000) throw new Exception("... file size is too big - 15MB max");
            System.Console.WriteLine("-> Done!");

            System.Console.WriteLine("Game transfering ...");
            sendData(data, port, 0, (int)data.Length);
            System.Console.WriteLine("-> Done!");

            System.Console.WriteLine("Game starting ...");
            if (arg.Equals("-sms"))
            {
                port.Write("*rs"); // MasterSystem ROM
            }
            else if (arg.Equals("-os"))
            {
                port.Write("*ro"); // EverDrive Application
            }
            else if (arg.Equals("-cd"))
            {
                port.Write("*rc"); // Mega-CD addon ROM
            }
            else if (arg.Equals("-m10"))
            {
                port.Write("*rM"); // SMD "big-ROM" (10M w/o mapper)
            }
            else if (arg.Equals("-ssf"))
            {
                port.Write("*rS"); // SMD ROM with SSF-mapper
            }
            else if (arg.Equals("-32x"))
            {
                port.Write("*r3"); // 32X addon ROM
            }
            else if (arg.Equals("-smd"))
            {
                port.Write("*rm"); // Generic SMD ROM
            }
            else
            {
                throw new Exception("... invalid option value");
            }

            if (port.ReadByte() != 'k') throw new Exception("... unexpected response");

            System.Console.WriteLine("-> Done!");
        }


        static void sendData(byte[] data, SerialPort port, int offset, int len)
        {
            int block_len = 0x8000; // len % 0x10000 == 0 ? 0x10000 : 0x8000;

            System.Console.WriteLine("Upload data to Mega EverDrive ...");
            DateTime dt = DateTime.Now;
            for (int i = 0; i < len; i += block_len)
            {
                port.Write(data, i + offset, block_len);
                if (i % 65536 == 0) Console.Write("*");
            }
            System.Console.WriteLine("");
            long tm = (System.DateTime.Now - dt).Ticks;
            System.Console.WriteLine("Time: " + ((double)(tm / 10000) / 1000).ToString("F") + "sec");
            if (port.ReadByte() != 'd') throw new Exception("... unexpected response");
        }


        static SerialPort searchCart()
        {
            string[] port_list = SerialPort.GetPortNames();
            SerialPort port = null;

            for (int i = 0; i < port_list.Length; i++)
            {
                try
                {
                    port = new SerialPort(port_list[i]);
                    port.ReadTimeout = 200;
                    port.WriteTimeout = 200;
                    port.Open();
                    port.ReadExisting();
                    port.Write("    *T");

                    if (port.ReadByte() == (byte)'k') return port;
                    port.Close();
                }
                catch (Exception)
                {
                    if (port.IsOpen) port.Close();
                }

            }

            throw new Exception("Mega EverDrive is not detected: check power, connect, driver and etc ... reboot EverDrive!");
        }


        static void v2_sysTools(string[] args, SerialPort port)
        {
            port.ReadTimeout = 3000;
            port.WriteTimeout = 3000;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains(".RBF") || args[i].Contains(".rbf"))
                {
                    v2_loadFpga(args[i], port);
                    //Thread.Sleep(500);
                    port.Write("*T");
                    if (port.ReadByte() != 'k') throw new Exception("... unexpected response");

                }
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains("MEGA.BIN"))
                {
                    v2_loadOS(args[i], port);
                    //Thread.Sleep(500);
                    port.Write("*T");
                    if (port.ReadByte() != 'k') throw new Exception("... unexpected response");
                }
            }
        }


        static void v2_loadOS(string filename, SerialPort port)
        {
            System.Console.WriteLine("OS loading ...");
            FileStream f = File.OpenRead(filename);
            byte[] buff = new byte[f.Length];
            f.Read(buff, 0, buff.Length);
            f.Close();

            port.Write("*o");
            port.Write(new byte[] { (byte)(buff.Length / 512 >> 8), (byte)(buff.Length / 512) }, 0, 2);

            int block_len = 2048;
            for (int i = 0; i < buff.Length; i += block_len)
            {
                port.Write(buff, i, block_len);
                if (i % 8192 == 0) Console.Write("*");
            }

            port.Write("*R");
            System.Console.WriteLine("");
            System.Console.WriteLine("-> Done!");

        }


        static void v2_loadFpga(string filename, SerialPort port)
        {
            System.Console.WriteLine("FPGA config loading ...");
            FileStream f = File.OpenRead(filename);
            byte[] buff = new byte[0x18000];
            for (int i = 0; i < buff.Length; i++) buff[i] = 0xff;
            f.Read(buff, 0, buff.Length);
            f.Close();

            port.Write("*f");
            port.Write(new byte[] { (byte)(buff.Length / 2 >> 8), (byte)(buff.Length / 2) }, 0, 2);

            int block_len = 2048;
            for (int i = 0; i < buff.Length; i += block_len)
            {
                port.Write(buff, i, block_len);
                if (i % 8192 == 0) Console.Write("*");
            }

            System.Console.WriteLine("");
            System.Console.WriteLine("-> Done!");

        }

    }

}
