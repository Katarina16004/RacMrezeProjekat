using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Potapanje_Brodova;
using System.Security;
using System.Threading;

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
        private static int rezultatGadjanja;

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
            byte[] prijemniBafer = new byte[128];
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
                        binarnaPoruka = Encoding.UTF8.GetBytes("Neuspesno ubacen na server. razlog: \n" + errorMessage + '\0');
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
                binarnaPoruka = Encoding.UTF8.GetBytes("SPREMAN" + '\0');
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
                if(!igracNaPotezu.izgubio)
                {
                    Igrac protivnik = null;
                    ObavestiIgrace(igracNaPotezu);

                    //potez trenutnog igraca
                    string imeProtivnika;
                    do
                    {
                        imeProtivnika = CekajNaPotez(igracNaPotezu);
                        protivnik = Igraci.FirstOrDefault(i => i.ime == imeProtivnika);

                        if (protivnik == null || protivnik.izgubio)
                        {
                            Console.WriteLine("Igrac ne postoji ili je vec eliminisan. Pokusaj ponovo");
                            imeProtivnika = null;
                        }
                    } while (imeProtivnika == null);

                    int polje=-1;
                    string poljeProtivnika;

                    bool krajPoteza;
                    do
                    {
                        PosaljiTabluGadjanja(igracNaPotezu, protivnik);
                        do
                        {
                            do
                                poljeProtivnika = CekajNaPotez(igracNaPotezu);
                            while (poljeProtivnika == null);
                            polje = int.Parse(poljeProtivnika);
                            krajPoteza = NapadniProtivnika(igracNaPotezu, imeProtivnika, polje);
                        } while (rezultatGadjanja==0);

                        PosaljiTabluGadjanja(igracNaPotezu, protivnik);

                    } while (!krajPoteza); //dok se pogadja polje igra isti igrac

                    if (krajPartije)
                    {
                        GlasanjeNovaIgra();
                        return;
                    }
                }
                trenutniIgrac = (trenutniIgrac + 1) % Igraci.Count;
            } while (!krajPartije);
        }
        private static void PosaljiTabluGadjanja(Igrac igrac, Igrac protivnik)
        {
            string tablaGadjanja = protivnik.PrikaziMatricuGadjana();
            byte[] tablaData = Encoding.UTF8.GetBytes(tablaGadjanja);
            try
            {
                igrac.socket.Send(tablaData);
                //Console.WriteLine("Uspesno poslata tabla gadjanja protivnika");
            }
            catch
            {
                Console.WriteLine("Greska pri slanju table gadjanja protivnika");
            }
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

        private static void ObjaviKrajPartije(Igrac pobednik)
        {
            string poruka = "Kraj partije! Igrac" + pobednik.ime + "je pobedio!";
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
            krajPartije=true;
        }
        private static void ObavestiIgrace(Igrac igracNaPotezu)
        {
            foreach (Igrac i in Igraci)
            {
                int dostupnihIgraca = 0;
                string poruka = "";

                if (i == igracNaPotezu)
                {
                    poruka = "\nIzaberi koga zelis da napadnes";
                    foreach (Igrac ig in Igraci)
                    {
                        if (ig.ime != i.ime && ig.izgubio == false)
                        {
                            poruka = poruka + "\n\t->" + ig.ime;
                            dostupnihIgraca++;
                        }                           
                    }
                    if (dostupnihIgraca == 0)
                        ObjaviKrajPartije(i);
                }
                else
                {
                    poruka = $"\n{igracNaPotezu.ime} je na potezu. Sacekajte..";
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

        //Salje svima poruku da li zele novu igru, ukoliko zele ide ispocetka, ukoliko ne kraj
        private static void GlasanjeNovaIgra()
        {
            string poruka = "Unesite 1 ukoliko zelite novu partiju, 2 ukoliko ne zelite:";
            byte [] message = Encoding.UTF8.GetBytes (poruka);
            foreach(Igrac i in Igraci)
            {
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

            int brojPrimljenihPoruka = 0;
            bool nova = true;
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
                            string primljena = Encoding.UTF8.GetString(buffer);
                            if (primljena.Contains("2"))
                            {
                                nova = false;   
                            }
                            Console.WriteLine("Primljena poruka od:" + s.RemoteEndPoint);
                            brojPrimljenihPoruka++;
                        }
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Greska u prijemu podmornica od {s.RemoteEndPoint}: {ex.Message}");
                    }
                }
            }

            krajPartije = nova;
        }

        //Igrac napada dok ne napravi Maksimalan broj uzastopnih gresaka ili dok ne pobedi
        // 1 Bira koga ce napasti
        // 2 Napada 
        // 3 Server obavestava kako je prosao napad
        // 4 ukoliko ima jos napada ide na korak 2
        private static bool NapadniProtivnika(Igrac trenutniIgrac,string imeProtivnika,int polje)
        {
            bool krajPoteza = false;
            Igrac Protivnik = null;
            int GadjajPolje = polje;
            string poruka;
            
            foreach (Igrac i in Igraci)
            {
                if(i.ime == imeProtivnika)
                {
                    Protivnik = i;
                    break;
                }
            }

            rezultatGadjanja = Protivnik.AzurirajMatricu(GadjajPolje);
            string info=""; 
            switch (rezultatGadjanja)
            {
                case 0:
                    poruka = "Vec napadnuto polje!";
                    krajPoteza = false;
                    info = $"\nPolje {polje} je vec gadjano. Izaberite drugo:";
                    break;
                case 1: //kada napadac promasi, sledeci igrac dobija potez. Ukoliko je napadac stigao do max broja gresaka, za njega je partija
                        //zavrsena i ceka rezultat. On do kraja partije ne moze da gadja nikoga, niti iko njega (zadatak 8)
                    poruka = "Promasaj!";
                    krajPoteza = true;
                    trenutniIgrac.brojPromasaja++;
                    info = $"\nBroj uzastopnih gresaka do sad je " +
                            $"{trenutniIgrac.brojPromasaja}, maksimalan broj je: {MaxUzastopnihGresaka}\n";

                    if (trenutniIgrac.brojPromasaja == MaxUzastopnihGresaka)
                    {
                        trenutniIgrac.izgubio = true;
                    }
                    break;
                case 2: //kada napadac potopi podmornicu protivniku, dobija sansu da gadja opet. Ukoliko je potopio sve podmornice, dobija sledeci potez
                        //da gadja nekog preostalog. (ili tu moze da se zavrsi njegov potez zbog jednostavnosti)
                    poruka = "Pogodak!";
                    info = $"\nPreostalo brodova protivniku je: {Protivnik.pozicije.Count}\n";

                    if (Protivnik.pozicije.Count == 0)
                    {
                        Protivnik.izgubio = true;
                        krajPoteza=true;
                    }
                    break;
                default:
                    poruka = "Greska!";
                    break;
            }

            byte[] message = Encoding.UTF8.GetBytes(poruka + info);
            //Posalji poruku Igracu kako je prosao potez
            try
            {
                trenutniIgrac.socket.Send(message);
                //Console.WriteLine($"Poruka poslata igracu {trenutniIgrac.ime}: {poruka}");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Greska pri slanju poruke igracu {trenutniIgrac.ime}: {ex.Message}");
            }

            ObavestiOstaleONapadu(trenutniIgrac, Protivnik, poruka);

            return krajPoteza;
        }

        private static void ObavestiOstaleONapadu(Igrac trenutniIgrac, Igrac protivnik, string ishod)
        {
            Thread.Sleep(300);
            string poruka = $"Igrac {trenutniIgrac.ime} -> {protivnik.ime}: {ishod}";
            Console.WriteLine(poruka);//ispis i na serveru
            byte [] message = Encoding.UTF8.GetBytes(poruka);
            foreach (Igrac i in Igraci)
            {
                if(i != trenutniIgrac)
                {
                    if(i==protivnik)
                    {
                        try
                        {
                            byte[] porukaZaProtivnika = Encoding.UTF8.GetBytes(trenutniIgrac.ime + " je gadjao vas i odigrao: " + ishod+"\nVasa tabla sada izgleda ovako:\n"+protivnik.PrikaziMatricu());
                            i.socket.Send(porukaZaProtivnika);
                            //Console.WriteLine($"Poruka poslata igracu {i.ime}: {porukaZaProtivnika}");
                        }
                        catch
                        {
                            Console.WriteLine($"Greska pri slanju poruke igracu {i.ime}");
                        }
                    }
                    else
                    {
                        try
                        {
                            i.socket.Send(message);
                            //Console.WriteLine($"Poruka poslata igracu {i.ime}: {poruka}");
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"Greska pri slanju poruke igracu {i.ime}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
