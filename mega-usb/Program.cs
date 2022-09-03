using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
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

                Console.WriteLine("");
                Console.WriteLine("============================");
                Console.WriteLine("Mega EverDrive usb-tool-plus");
                Console.WriteLine("============================");
                Console.WriteLine("");

                if ((args.Length == 1) && ((args[0].Contains("?") || args[0].Contains("-help"))))
                {
                    Console.WriteLine("mega-usb <filename.ext> [-options]");
                    Console.WriteLine("");
                    Console.WriteLine("Fast usage by fixed \"filename.ext\" (w/o options):");
                    Console.WriteLine("");
                    Console.WriteLine("     *.RBF - Load FPGA config to Mega EverDrive v2(x7), x3 ,x5");
                    Console.WriteLine("  MEGA.BIN - Load OS 68k image to Mega EverDrive v2(x7), x3 ,x5");
                    Console.WriteLine("MEGAOS.bin - Load OS 68k image to Mega EverDrive v1");
                    Console.WriteLine("     *.SMS - Load as MasterSystem ROM");
                    Console.WriteLine("       *.* - Load as MegaDrive (SMD) Generic ROM (up to 4MB)");
                    Console.WriteLine("");
                    Console.WriteLine("(for v2 - FPGA and OS file possible loading by one command,");
                    Console.WriteLine(" as an option in case absense sd-card in Mega EverDrive ...)");
                    Console.WriteLine("");
                    Console.WriteLine("Options detail usage for any \"filename.ext\" (excl. see above):");
                    Console.WriteLine("");
                    Console.WriteLine(" -smd      - Load file as SMD Generic ROM (default)         | *rm");
                    Console.WriteLine(" -m10      - Load file as SMD \"big-ROM\" (10MB w/o mapper)   | *rM");
                    Console.WriteLine(" -ssf      - Load file as SMD ROM with SSF-mapper           | *rS");
                    Console.WriteLine("             (Also used for full access EverDrive functions)");
                    Console.WriteLine(" -cd       - Load file as Mega-CD addon ROM (BIOS)          | *rc");
                    Console.WriteLine(" -32x      - Load file as 32X addon ROM                     | *r3");
                    Console.WriteLine(" -sms      - Load file as MasterSystem ROM                  | *rs");
                    Console.WriteLine(" -os       - Load file as EverDrive Application             | *ro");
                    Console.WriteLine(" -m <mode> - Relaunching loaded software, by raw start code");
                    Console.WriteLine("             (W/o filename, full 3-symb. code *r? see above)");
                    Console.WriteLine(" -o        - Load file as OS 68k image to Mega EverDrive v1");
                    Console.WriteLine("             (Equivalent to MEGAOS.bin without options)");
                    Console.WriteLine(" -fo       - Similar to the previous one, but also writes");
                    Console.WriteLine("             a Firmware block from unified image (v1 only!)");
                    Console.WriteLine("");
                    Console.WriteLine("Example (w/o SD-card, the sequence of actions is as follows):");
                    Console.WriteLine("");
                    Console.WriteLine("mega-usb SVP.RBF           - Load FPGA config");
                    Console.WriteLine("mega-usb MEGA.BIN          - Load OS image");
                    Console.WriteLine("mega-usb MEGA.RBF MEGA.BIN - Load FPGA & OS image");
                    Console.WriteLine("mega-usb GAMEDUMP.BIN      - Load SMD ROM");
                    Console.WriteLine("");
                    Console.WriteLine("* Before next ROM load - press Reset key on EverDrive or SMD,");
                    Console.WriteLine("   or return to the main menu in any other way ...");
                    Console.WriteLine("");
                    Console.WriteLine("(?) OpenSource build 2022");
                    return; // Exit form this tool ...
                }

                Console.WriteLine("Testing link ...");
                port = searchCart();
                Console.WriteLine("-> Mega EverDrive detected on " + port.PortName);

                if (args.Length == 0)
                {
                    Console.WriteLine("");
                    Console.WriteLine("For take detail about use - run with /? or -help");
                    return; // Exit form this tool ...
                }

                for (int i = 0; i < args.Length; i++) // Predefined filename of FPGA or OS content for v2
                {
                    if (args[i].Contains("MEGA.BIN") || args[i].Contains(".RBF") || args[i].Contains(".rbf"))
                    {
                        Console.WriteLine("Selected Mega EverDrive v2 hardware init");
                        v2_sysTools(args, port);
                        port.Close();
                        return; // Exit form this tool ...
                    }
                }

                if (args.Length == 1) // One argument ...
                {
                    if (args[0].Contains("-c")) // Console mode
                    {
                        // Future Feature ;-))
                        return;
                    }
                    else // Only filename for game (or v1 OS+Firmware)
                    {
                        option = args[0].Contains("MEGAOS.bin") ? "-o" : args[0].Contains(".sms") | args[0].Contains(".SMS") ? "-sms" : "-smd";
                    }
                }
                else if (args.Length == 2) // Two argument ...
                {
                    if (args[0].Contains("-m")) // Manual select start-mode
                    {
                        option = args[1];
                        Console.WriteLine("Executing start command with code : " + option);
                        port.Write(option);
                        if (port.ReadByte() != 'k') throw new Exception("... unexpected response or bad code");
                        else Console.WriteLine("-> Done!");
                        return;
                    }
                    else // Filename & Option (Regular variant)
                    {
                        option = args[1];
                    }
                }
                else throw new Exception("... too many command-line argument");

                FileStream f = File.OpenRead(args[0]); // Reading file by name from args[0]
                int len = (int)f.Length;
                if (len % 65536 != 0) len = len / 65536 * 65536 + 65536;
                byte[] buff = new byte[len]; // Content
                f.Read(buff, 0, (int)f.Length);
                f.Close();
                
                // Auto detect adsress-model - 2-nd step (if not manual selected)
                if (args.Length == 1)
                {
                    if (buff[261] == 'S' && buff[262] == 'S' && buff[263] == 'F' && buff[264] == ' ') option = "-ssf";
                    else if (len > 4096 * 1024) option = "-m10";
                }

                // Final checks and transition to action ;-))
                if (option.Equals("-o") || option.Equals("-fo"))
                {
                    Console.WriteLine("Selected Mega EverDrive v1 hardware init");
                    v1_sysTools(option, buff, port);
                }
                else
                {
                    uploadGame(buff, port);
                    runGame(option, port);
                }

            }
            catch (Exception x)
            {
                Console.WriteLine("\nERROR: " + x.Message);
            }
        }

        static void uploadGame(byte[] data, SerialPort port) // *g - command
        {
            byte[] tx = new byte[1];
            tx[0] = (byte)(data.Length / 512 / 128);

            Console.WriteLine("Game upload preparing ...");
            if (data.Length > 0xf00000) throw new Exception("... file size is too big - 15MB max");
            else Console.WriteLine("-> Size: " + data.Length);
            port.ReadTimeout = 1000;
            port.WriteTimeout = 1000;
            port.Write("*g");
            port.Write(tx, 0, 1);
            if (port.ReadByte() != 'k') throw new Exception("... unexpected response: link disconnected?");
            else Console.WriteLine("-> Link is Ok!");

            Console.WriteLine("Game transfering to Mega EverDrive ...");
            sendData(data, port, 0, (int)data.Length);
        }

        static void sendData(byte[] data, SerialPort port, int offset, int len)
        {
            int block_len = 0x8000; // len % 0x10000 == 0 ? 0x10000 : 0x8000;

            DateTime dt = DateTime.Now;
            for (int i = 0; i < len; i += block_len)
            {
                port.Write(data, i + offset, block_len);
                if (i % 65536 == 0) Console.Write("*");
            }
            Console.WriteLine("");
            long tm = (System.DateTime.Now - dt).Ticks;
            Console.WriteLine("Time: " + ((double)(tm / 10000) / 1000).ToString("F") + "sec");
            if (port.ReadByte() != 'd') throw new Exception("... unexpected response");
            else Console.WriteLine("-> Complete!");
        }

        static void runGame(string arg, SerialPort port)
        {
            Console.WriteLine("Selected is " + arg + " cart mode");
            Console.WriteLine("Game starting ...");

            switch (arg)
            {
                case "-smd":
                    port.Write("*rm"); // Generic SMD ROM
                    break;
                case "-m10":
                    port.Write("*rM"); // SMD "big-ROM" (10M w/o mapper)
                    break;
                case "-ssf":
                    port.Write("*rS"); // SMD ROM with SSF-mapper
                    break;
                case "-cd":
                    port.Write("*rc"); // Mega-CD addon ROM (BIOS)
                    break;
                case "-32x":
                    port.Write("*r3"); // 32X addon ROM
                    break;
                case "-sms":
                    port.Write("*rs"); // MasterSystem ROM
                    break;
                case "-os":
                    port.Write("*ro"); // EverDrive Application
                    break;
                default:
                    throw new Exception("... invalid option value");
            }

            if (port.ReadByte() != 'k') throw new Exception("... unexpected response");
            else Console.WriteLine("-> Done!");
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
                    port.Write("  *T");
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

        static void testLink(SerialPort port) // *T - command
        {
            port.Write("*T");
            if (port.ReadByte() != 'k') throw new Exception("... unexpected response");
        }

        static void v2_sysTools(string[] args, SerialPort port) // MEGA.RBF &| MEGA.BIN transferred
        {
            port.ReadTimeout = 3000;
            port.WriteTimeout = 3000;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains(".RBF") || args[i].Contains(".rbf"))
                {
                    v2_loadFPGA(args[i], port);
                    //Thread.Sleep(500);
                    testLink(port);
                }
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains("MEGA.BIN"))
                {
                    v2_loadOS(args[i], port);
                    //Thread.Sleep(500);
                    testLink(port);
                }
            }
        }

        static void v2_loadFPGA(string filename, SerialPort port) // *f - command
        {
            Console.WriteLine("FPGA config loading ...");
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

            Console.WriteLine("");
            Console.WriteLine("-> Done!");
        }

        static void v2_loadOS(string filename, SerialPort port) // *o - command & *R - command
        {
            Console.WriteLine("OS 68k loading ...");
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
            Console.WriteLine("");
            Console.WriteLine("-> Done!");
        }

        static void v1_sysTools(string arg, byte[] data, SerialPort port) // for Mega EverDrive v1 (not X7)
        {
            if (data.Length > 0x100000) throw new Exception("OS file is too big: wrong file?");
            port.ReadTimeout = 1000;
            port.WriteTimeout = 1000;
            byte[] tx = new byte[1];

            if (arg.Equals("-fo"))
            {
                Console.WriteLine("Firmware part loading...");
                tx[0] = (byte)(64 + 32);
                port.Write("*f");
                port.Write(tx, 0, 1);
                if (port.ReadByte() != 'k') throw new Exception("... unexpected response");

                sendData(data, port, 0x8400, 0x18000); // Send Firmware - included in unified file with OS

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
                        testLink(port);
                        break;
                    }
                    catch (Exception) { }
                }

                port.ReadTimeout = 1000;
                port.WriteTimeout = 1000;
                Console.WriteLine("-> Done!");
            }

            Console.WriteLine("OS loading ...");
            tx[0] = (byte)(data.Length / 512 / 128);
            port.Write("*o");
            port.Write(tx, 0, 1);
            if (port.ReadByte() != 'k') throw new Exception("... unexpected response");

            sendData(data, port, 0, data.Length); // Send OS            
        }

    }

}
