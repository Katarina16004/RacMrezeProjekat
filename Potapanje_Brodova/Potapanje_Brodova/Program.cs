using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Potapanje_Brodova;

namespace Server
{
    internal class Program
    {
        private static List<Klijent> Klijenti = new List<Klijent>();
        private static List<Igrac> Igraci = new List<Igrac>();
        private static List<Socket> readySockets = null;

        private static List<Socket> clientSockets = null;

        public static Socket serverSocket = null;
        private static int MaxBrojIgraca = 0;
        private static int VelicinaTable = 0;
        private static int MaxUzastopnihGresaka = 0;
        private static bool NovaIgra = true;
        private static bool krajPartije = false;

        static void Main(string[] args)
        {

            Console.WriteLine("Dobrodosli na server!");
            Console.WriteLine("Unesite broj igraca koji ce da igraju:");
            int.TryParse(Console.ReadLine(), out MaxBrojIgraca);
            Console.WriteLine("Cekam prijave Igraca:");

            UcitajIgrace();
            UspostaviTCPKonekciju();


            while (NovaIgra)
            {
                IncijalizujTable();
                PosaljiKlijentimaTable();
                ZapocniIgru();
            }

            ZatvoriUticnice();


            Console.ReadKey();
        }

        //Ucitava igrace, proverava da li neko sa tim imenom vec prijavljen, ako nije ubacuje,
        //ako jeste odbija prijavu i salje poruku nazad
        static void UcitajIgrace()
        {

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
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

                    foreach (Klijent k in Klijenti)
                    {
                        if (k.Ime == ime)
                        {
                            Console.WriteLine("Vec postoji client sa datim imenom");
                            errorMessage = "Vec postoji client sa datim imenom";
                            postojiKlijent = true;
                            break;
                        }
                    }

                    if (!postojiKlijent)
                    {
                        Klijenti.Add(klijent);
                        Console.WriteLine("Ubacen klijent!");
                    }

                    Console.WriteLine("Do sada su ubaceni:");
                    foreach (Klijent k in Klijenti)
                    {
                        Console.WriteLine(k);
                    }


                    if (errorMessage == null)
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
            foreach (Klijent k in Klijenti)
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

        //Uspostavljanje TCP konekcije sa svim igracima, slanje informacija o igri,
        private static void UspostaviTCPKonekciju()
        {
            // uspostavljanje TCP konekcije
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 5001);
            serverSocket.Bind(serverEP);
            serverSocket.Listen(MaxBrojIgraca);
            serverSocket.Blocking = false;

            clientSockets = new List<Socket>();
            readySockets = new List<Socket>();

            while (clientSockets.Count != MaxBrojIgraca)
            {
                readySockets.Clear();
                readySockets.Add(serverSocket);

                Socket.Select(readySockets, null, null, 1000);

                if (readySockets.Count > 0)
                {
                    Socket clientSocket = serverSocket.Accept();
                    clientSocket.Blocking = false;
                    clientSockets.Add(clientSocket);
                    Console.WriteLine($"Novi klijent povezan: {clientSocket.RemoteEndPoint}");
                    Igraci.Add(new Igrac(clientSocket, Igraci.Count, VelicinaTable));
                }
            }

            // slanje informacija o igri klijentima
            int brPodmornica = VelicinaTable * VelicinaTable - MaxUzastopnihGresaka;
            string info = $"Velicina table: {VelicinaTable}, maksimalan broj gresaka: {MaxUzastopnihGresaka}, broj podmornica: {brPodmornica}";
            byte[] infoMessage = Encoding.UTF8.GetBytes(info);

            foreach (Socket clientSocket in clientSockets)
            {
                try
                {
                    clientSocket.Send(infoMessage);
                    Console.WriteLine($"Poruka poslata klijentu: {clientSocket.RemoteEndPoint}");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Greska pri slanju poruke klijentu {clientSocket.RemoteEndPoint}: {ex.Message}");
                }
            }

        }

        private static void IncijalizujTable()
        {
            // obrada podmornica od klijenata
            int brojPrimljenihPoruka = 0;

            while (brojPrimljenihPoruka < clientSockets.Count)
            {
                readySockets.Clear();
                foreach (Socket clientSocket in clientSockets)
                {
                    readySockets.Add(clientSocket);
                }

                Socket.Select(readySockets, null, null, 1000);

                foreach (Socket s in readySockets)
                {
                    byte[] buffer = new byte[1024];
                    try
                    {
                        int messLength = s.Receive(buffer);

                        if (messLength > 0)
                        {
                            string poruka = Encoding.UTF8.GetString(buffer, 0, messLength);
                            string[] delovi = poruka.Split('|');
                            string ime = delovi[0];
                            poruka = delovi[1];
                            Console.WriteLine($"Podmornice od {ime}: {poruka}");
                            string[] pozS = poruka.Split(',');
                            List<int> listaPoz = new List<int>();
                            for (int i = 0; i < pozS.Length; i++)
                                listaPoz.Add(int.Parse(pozS[i]));

                            //Svaki socket je vezan za specificnog igraca i moramo potraziti koji je igrac u pitanju
                            foreach (Igrac i in Igraci)
                            {
                                if (i.socket == s)
                                {
                                    i.DodajPodmornice(listaPoz, ime);
                                    break;
                                }
                            }
                            Console.WriteLine("Primljena poruka od:" + ime);
                            brojPrimljenihPoruka++;
                        }
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Greska u prijemu podmornica od {s.RemoteEndPoint}: {ex.Message}");
                    }
                }
            }
        }

        private static void ZatvoriUticnice()
        {
            foreach (Igrac i in Igraci)
            {
                i.socket.Close();
            }
            serverSocket.Close();
        }

        private static void PosaljiKlijentimaTable()
        {
            foreach (Igrac i in Igraci)
            {
                try
                {
                    byte[] poruka = i.SerijalizujMatricu();
                    i.socket.Send(poruka);
                    Console.WriteLine($"Poruka poslata klijentu: {i.ime} u {i.socket.RemoteEndPoint}");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Greska pri slanju poruke klijentu {i.ime}: {ex.Message}");
                }
            }
        }

        private static void ZapocniIgru()
        {
            int trenutniIgrac = 0;
            do
            {
                Igrac igracNaPotezu = Igraci[trenutniIgrac];
                ObavestiIgrace(igracNaPotezu);

                //potez trenutnog igraca
                string odgovor = CekajNaPotez(igracNaPotezu);

                if (!string.IsNullOrEmpty(odgovor))
                {
                    string[] delovi = odgovor.Split('|');
                    string imeProtivnika = delovi[0];
                    int polje = int.Parse(delovi[1]);

                    //NapadniProtivnika();

                    if (krajPartije)
                    {
                        //ObjaviKrajPartije();
                        //GlasanjeNovaIgra();
                        return;
                    }
                }

                trenutniIgrac = (trenutniIgrac + 1) % Igraci.Count;
            } while (!krajPartije);
        }

        private static string CekajNaPotez(Igrac igracNaPotezu)
        {
            Socket socket = igracNaPotezu.socket;
            byte[] buffer = new byte[1024];
            int bytesReceived = 0;
            string odgovor = "";

            while (string.IsNullOrEmpty(odgovor))
            {
                List<Socket> readySockets = new List<Socket> { socket };
                Socket.Select(readySockets, null, null, 1000);

                if (readySockets.Count > 0)
                {
                    try
                    {
                        bytesReceived = socket.Receive(buffer);
                        if (bytesReceived > 0)
                        {
                            odgovor = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                            Console.WriteLine($"Primljen odgovor od {igracNaPotezu.ime}: {odgovor}");
                        }
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Greska pri prijemu podataka: {ex.Message}");
                    }
                }
            }
            return odgovor;
        }

        private static void ObjaviKrajPartije()
        {
            string poruka = "Kraj partije!";
            foreach (Igrac i in Igraci)
            {
                try
                {
                    byte[] message = Encoding.UTF8.GetBytes(poruka);
                    i.socket.Send(message);
                    Console.WriteLine($"Poruka poslana igracu {i.ime}: {poruka}");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Greska pri slanju poruke igracu {i.ime}: {ex.Message}");
                }
            }
        }
        private static void ObavestiIgrace(Igrac igracNaPotezu)
        {
            foreach (Igrac i in Igraci)
            {
                string poruka = "";

                if (i == igracNaPotezu)
                {
                    poruka = $"Izaberi koga zelis da napadnes";
                    foreach (Igrac ig in Igraci)
                    {
                        if (ig.ime != i.ime)
                            poruka = poruka + "\n\t->" + ig.ime;
                    }
                }
                else
                {
                    poruka = $"{igracNaPotezu.ime} je na potezu. Sacekajte..";
                }

                byte[] message = Encoding.UTF8.GetBytes(poruka);

                try
                {
                    i.socket.Send(message);
                    Console.WriteLine($"Poruka poslata igracu {i.ime}: {poruka}");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Greska pri slanju poruke igracu {i.ime}: {ex.Message}");
                }
            }
        }

        private static void OdaberiProtivnika()
        {
            throw new NotImplementedException();
        }


        //Salje svima poruku da li zele novu igru, ukoliko zele ide ispocetka, ukoliko ne kraj
        private static void GlasanjeNovaIgra()
        {
            throw new NotImplementedException();
        }

        //Igrac napada dok ne napravi Maksimalan broj uzastopnih gresaka ili dok ne pobedi
        // 1 Bira koga ce napasti
        // 2 Napada 
        // 3 Server obavestava kako je prosao napad
        // 4 ukoliko ima jos napada ide na korak 2
        private static void NapadniProtivnika(Igrac trenutniIgrac)
        {
            throw new NotImplementedException();
        }
    }
}
