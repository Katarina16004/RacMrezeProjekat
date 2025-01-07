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

            UspostaviTCPKonekciju();

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

        //Salje prijavu server i ceka odgovor, u slucaju da je pozitivan odgovor,
        //nastavlja sa izvrsavanjem, u slucaju negativnog odgovara vraca na meni
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

        //TODO uspostaviti konekciju sa serverom, nakon toga postaviti brodove na tablu
        private static void UspostaviTCPKonekciju()
        {
            Thread.Sleep(1000);
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ServerEP = new IPEndPoint(IPAddress.Parse("192.168.56.1"), 55358);
            byte[] buffer = new byte[1024];
            clientSocket.Connect(ServerEP);

            while (true)
            {
                Console.WriteLine("Unesite poruku");
                try
                {
                    string poruka = Console.ReadLine();
                    int brBajta = clientSocket.Send(Encoding.UTF8.GetBytes(poruka));

                    if (poruka == "kraj")
                        break;

                    brBajta = clientSocket.Receive(buffer);

                    if (brBajta == 0)
                    {
                        Console.WriteLine("Server je zavrsio sa radom");
                        break;
                    }

                    string odgovor = Encoding.UTF8.GetString(buffer);

                    Console.WriteLine(odgovor);
                    if (odgovor == "kraj")
                        break;

                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do greske tokom slanja:\n{ex}");
                    break;
                }

            }

            Console.WriteLine("Klijent zavrsava sa radom");
            clientSocket.Close();
        }


    }
}

