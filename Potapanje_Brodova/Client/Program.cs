using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        public static string ime = null;

        static void Main(string[] args)
        {
            Console.WriteLine("Pozdrav od Clienta");
            PrikaziMeni();

            
            Console.ReadKey();
        }
        static void PrikaziMeni()
        {
            int x;
            ime = null;

            bool unos = false;
            do
            {
               Console.WriteLine("Dobrodosli u potapanje brodova! \n Pritisnite sledece opcije:" +
                           "\n 1) Nova igra \n 2) Izlaz");
                int.TryParse(Console.ReadLine(), out x);
                switch (x)
                {
                    case 1:
                        UnesiIme();
                        unos = true;
                        Prijava();
                        break;
                    case 2:
                        Console.WriteLine("Dovidjenja!");
                        Thread.Sleep(1000);
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Greska!");
                        break;
                }
            } while (!unos);
        }

        static void UnesiIme()
        {
            Console.WriteLine("Uneiste svoje ime:");
            ime = Console.ReadLine(); 
            Console.WriteLine("Ucitavanje...");
            Thread.Sleep(1000);

        }

        static void Prijava() 
        {
            if (ime == null)
                return;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint destination = new IPEndPoint(IPAddress.Parse("192.168.56.1"), 60002);
            byte[] buffer = new byte[4096];
            buffer = Encoding.UTF8.GetBytes("PRIJAVA"+ime);
            byte[] buffer2 = new byte[1024];

            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Parse("192.168.56.1"),0);

            try
            {
                int brBajta = socket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, destination);
                int primljena = socket.ReceiveFrom(buffer2, ref posiljaocEP);
                string poruka = Encoding.UTF8.GetString(buffer2);
                Console.WriteLine(poruka);

                if (poruka.Contains("Neuspesno"))
                {
                    PrikaziMeni();
                }
                
                Console.WriteLine("Cekamo na prijavu ostalih igraca!");
            
            }
            catch (Exception ex)
            {
                Console.WriteLine("Desila se greska prilikom slanja poruke! \n " + ex.ToString());
            }
            finally
            {
                socket.Close();
            }

        }
    }
}

