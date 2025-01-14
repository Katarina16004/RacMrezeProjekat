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
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace Server
{
    internal class Program
    {
        public static string ime = null;
        public static Socket clientSocket = null;
        private static int brojPodmornica=0;
        private static int velTable = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Pozdrav od Clienta");
            PrikaziMeni();

            UspostaviTCPKonekciju();
            zatvoriTCPKonenciju();

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
                           "\n 1) Nova igra \n 2) Izlaz \n 3) Pokreni jos 10 klijenata");
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
                    case 3:
                        UpaliKlijente();
                        break;
                    default:
                        Console.WriteLine("Greska!");
                        break;
                }
            } while (!unos);
        }

        static void UnesiIme()
        {
            Console.WriteLine("Unesite svoje ime:");
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
                string poruka;
                int brBajta = socket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, destination);
                do
                {
                    int primljena = socket.ReceiveFrom(buffer2, ref posiljaocEP);
                    poruka = Encoding.UTF8.GetString(buffer2);
                    Console.WriteLine(poruka.TrimEnd());
                
                } while (!poruka.Contains("SPREMAN") && !poruka.Contains("Neuspesno"));


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

        //Uspostavljanje TCP konekcije sa serverom, prijem informacija o igri, slanje pozicija svojih podmornica
        private static void UspostaviTCPKonekciju()
        {
            Thread.Sleep(1000);
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ServerEP = new IPEndPoint(IPAddress.Parse("192.168.56.1"), 5001);
            byte[] buffer = new byte[1024];
            Random random = new Random();
            int brPokusaja = 0;

            while (true)
            {
                try
                {
                    clientSocket.Connect(ServerEP);
                    Console.WriteLine("Connected to server.");
                    break; 
                }
                catch (SocketException e)
                {
                    Console.WriteLine($"SocketException: {e.Message}");
                    Console.WriteLine("Pokusavam da se povezem na server...");
                    Thread.Sleep(random.Next(10, 100));
                    if (++brPokusaja == 10) 
                    {
                        Console.WriteLine("Neuspeno povezivanje na server");
                        zatvoriTCPKonenciju();
                        break;
                    }
                }
            }

            //primanje informacija o igri
            try
            {
                byte[] dataBuffer = new byte[256];
                int bytesRead = clientSocket.Receive(dataBuffer);
                string message = Encoding.UTF8.GetString(dataBuffer, 0, bytesRead);
                Console.WriteLine("Primljena poruka: " + message);

                string[] delovi = message.Split(' ');
                brojPodmornica = int.Parse(delovi[delovi.Length - 1]);
                string velTableS = delovi[2].Remove(delovi[2].Length - 1);
                velTable=int.Parse(velTableS);
            }
            catch (SocketException e)
            {
                Console.WriteLine($"Greska u konekciji! {e}");
                zatvoriTCPKonenciju();
            }

            //slanje podmornica severu
            List<int> pozicije=UnosPodmornica();

            byte[] podmornice = Encoding.UTF8.GetBytes(string.Join(",", pozicije));
            try
            {
                clientSocket.Send(podmornice);
                Console.WriteLine("Podmornice su uspesno poslate serveru.");
            }
            catch (SocketException e)
            {
                Console.WriteLine($"Greška prilikom slanja podmornica: {e.Message}");
            }

        }
        private static List<int> UnosPodmornica()
        {
            List<int> pozicije=new List<int>();
            int brUnetih = 0;
            Console.WriteLine($"Unesite pozicije vasih podmornica:");
            while(brUnetih<brojPodmornica)
            {
                string unos = Console.ReadLine();
                if (int.TryParse(unos, out int pozicija))
                {
                    if (pozicija >= 1 && pozicija <= velTable * velTable)
                    {
                        if (!pozicije.Contains(pozicija))
                        {
                            pozicije.Add(pozicija);
                            brUnetih++;
                        }
                        else
                        {
                            Console.WriteLine("Na toj poziciji vec imate podmornicu!");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Pozicija mora biti u opsegu od 1 do {velTable * velTable}");
                    }
                }
                else
                {
                    Console.WriteLine("Unesite broj pozicije!");
                }
            }
            return pozicije;
        }
        private static void zatvoriTCPKonenciju()
        {
            Console.ReadKey();
            Console.WriteLine("Klijent zavrsava sa radom");
            clientSocket.Close();
        }
        private static void UpaliKlijente()
        {
            for (int i = 0; i < 10; i++)
            {
                string clientPath = "Client.exe";
                Process klijentProces = new Process(); 
                klijentProces.StartInfo.FileName = clientPath;
                klijentProces.StartInfo.Arguments = $"{i + 1}";   
                klijentProces.Start(); 
                Console.WriteLine($"Pokrenut klijent #{i + 1}");
            }
        }
    }
}

