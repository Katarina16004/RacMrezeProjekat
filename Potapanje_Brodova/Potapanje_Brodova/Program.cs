using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        private static List<Klijent> Klijenti = new List<Klijent>();

        private static int MaxBrojIgraca = 0;
        private static int VelicinaTable = 0;
        private static int MaxUzastopnihGresaka = 0;


        static void Main(string[] args)
        {
            
            Console.WriteLine("Dobrodosli na server!");
            Console.WriteLine("Unesite broj igraca koji ce da igraju:");
            int.TryParse(Console.ReadLine(), out MaxBrojIgraca);
            Console.WriteLine("Cekam prijave Igraca:");

            UcitajIgrace();
            UspostaviTCPKonekciju();

            Console.ReadKey();
        }

        //Ucitava igrace, proverava da li neko sa tim imenom vec prijavljen, ako nije ubacuje,
        //ako jeste odbija prijavu i salje poruku nazad
        static void UcitajIgrace()
        {

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 60002);
            serverSocket.Bind(serverEP);
            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] binarnaPoruka;
            byte[] prijemniBafer = new byte[1024];
            do
            {
                try
                {
                    int brBajta = serverSocket.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                    string poruka = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);
                    Console.WriteLine($"Pokusaj prijave od {posiljaocEP}");
                    string ime = poruka.Substring(7);
                    string errorMessage = null;
                    bool postojiKlijent = false;
                   
                    if (ime.Length == 0)
                    {
                        Console.WriteLine("Ime je prazno!");
                        errorMessage = "Ime je prazno!";
                        return;
                    }

                    Klijent klijent = new Klijent(ime, posiljaocEP);

                    foreach(Klijent k in Klijenti)
                    {
                        if(k.Ime == ime)
                        {
                            Console.WriteLine("Vec postoji client sa datim imenom");
                            errorMessage = "Vec postoji client sa datim imenom";
                            postojiKlijent= true;
                            break;
                        }
                    }

                    if(!postojiKlijent)
                    {
                        Klijenti.Add(klijent);
                        Console.WriteLine("Ubacen klijent!");
                    }

                    Console.WriteLine("Do sada su ubaceni:");
                    foreach (Klijent k in Klijenti) {
                        Console.WriteLine(k);
                    }

                    
                    if(errorMessage == null)
                    {
                        binarnaPoruka = Encoding.UTF8.GetBytes("Uspesno ubacen na server");
                    }
                    else
                    {
                        binarnaPoruka = Encoding.UTF8.GetBytes("Neuspesno ubacen na server. razlog: \n" + errorMessage);
                    }
                    brBajta = serverSocket.SendTo(binarnaPoruka, 0, binarnaPoruka.Length, SocketFlags.None, posiljaocEP); 

                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do greske tokom prijema poruke: \n{ex}");
                }


            } while (Klijenti.Count() < MaxBrojIgraca);


            UnesiParametreIgre();

             //PosaljiSignalSpreman
            foreach(Klijent k in Klijenti)
            {
               binarnaPoruka = Encoding.UTF8.GetBytes("SPREMAN");
               serverSocket.SendTo(binarnaPoruka, 0, binarnaPoruka.Length, SocketFlags.None, k.IPAdresa);
            }
            
            
            serverSocket.Close();
        }

        private static void UnesiParametreIgre()
        {
            Console.WriteLine("Svi igraci su spremni za igru!");
            Console.WriteLine("Unesite dimenziju table:");
            int.TryParse((string)Console.ReadLine(), out VelicinaTable);
            Console.WriteLine("Unesite maksimalan broj uzastopnih gresaka:");
            int.TryParse((string)Console.ReadLine(), out MaxUzastopnihGresaka);
        }

        //TODO potrebna konekacija sa svima preko jednog porta - multipleksiranje uticnice,
        //nakon toga, obavestiti igrace o osnovnim parametrima igre
        private static void UspostaviTCPKonekciju()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 5001);
            serverSocket.Bind(serverEP);
            serverSocket.Listen(10); 
            serverSocket.Blocking = false; 

            List<Socket> clientSockets = new List<Socket>();
            byte[] buffer = new byte[1024];

            do
            {
                if (serverSocket.Poll(100000, SelectMode.SelectRead)) // 0.1 sekunda timeout
                {
                    Socket newClient = serverSocket.Accept();
                    newClient.Blocking = false;
                    clientSockets.Add(newClient);
                    Console.WriteLine($"Novi klijent povezan: {newClient.RemoteEndPoint}");

                    string initialMessage = $"Velicina table je {VelicinaTable}, dok je maksimalan broj gresaka {MaxUzastopnihGresaka}";
                    newClient.Send(Encoding.UTF8.GetBytes(initialMessage));
                }
            }
            while (clientSockets.Count()!=MaxBrojIgraca);
            
            foreach (Socket clientSocket in clientSockets)
            {
                clientSocket.Close();
            }
            serverSocket.Close();
           
        }


    }
}
