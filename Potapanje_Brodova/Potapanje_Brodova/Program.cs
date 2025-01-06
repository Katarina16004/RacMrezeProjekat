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
        static int MaxBrojIgraca = 0;
        static void Main(string[] args)
        {
            
            Console.WriteLine("Dobrodosli na server!");
            Console.WriteLine("Uneiste broj igraca koji ce da igraju:");
            int.TryParse(Console.ReadLine(), out MaxBrojIgraca);
            Console.WriteLine("Cekam prijave Igraca:");


            ucitajIgrace();

            Console.ReadKey();
        }

        static void ucitajIgrace()
        {

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 60002);
            serverSocket.Bind(serverEP);
            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);

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
                            break;
                        }
                    }
                    
                    Klijenti.Add(klijent);
                    Console.WriteLine("Ubacen klijent!");
                    Console.WriteLine("Do sada su ubaceni:");
                    foreach (Klijent k in Klijenti) {
                        Console.WriteLine(k);
                    }

                    byte[] binarnaPoruka;
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
            serverSocket.Close();
        }
    }
}
