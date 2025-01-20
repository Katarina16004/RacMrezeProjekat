using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Potapanje_Brodova
{
    public class Igrac
    {
        public Socket socket {  get; set; }
        public int id { get;}
        public string ime { get; set; }
        public int brojPromasaja { get; set; }
        public List<int> pozicije { get; set; } //korisnik salje pozicije (1-dim)
        public int[,] matrica { get; set; }

        public bool PrethodniPogodak {  get; set; }

        public Igrac(Socket socket,int id, int dimenzija)
        {
            this.socket = socket;
            this.id = id;
            brojPromasaja = 0;
            pozicije = new List<int>();
            matrica = new int[dimenzija, dimenzija];
            this.ime = ime;
            this.PrethodniPogodak = true;
        }
        public void DodajPodmornice(List<int> pozicije,string ime)
        {
            this.pozicije = pozicije;
            this.ime = ime;
            inicijalizujBrodove();
        }

        private void inicijalizujBrodove()
        {
            foreach (var pozicija in pozicije)
            {
                int i = (pozicija - 1) / matrica.GetLength(0);
                int j = (pozicija - 1) % matrica.GetLength(1);
                matrica[i,j] = 1;
            }
           
        }

        public int AzurirajMatricu(int gadjanaPoz) //salje se pozicija (1-dim) koju protivnik gadja
        {

            int i = (gadjanaPoz - 1) / matrica.GetLength(0);
            int j = (gadjanaPoz - 1) % matrica.GetLength(1);
            if (matrica[i, j] == 0)
            {
                if (pozicije.Contains(gadjanaPoz))
                {
                    matrica[i, j] = 2; // ako se tu nalazila podmornica, znaci da je sada pogodjena
                    pozicije.Remove(gadjanaPoz);
                    return 2; //pogodjeno
                }
                else
                {
                    matrica[i, j] = 1; // ako se tu ne nalazi podmornica, znaci da je promasena
                    return 1; //promaseno
                }
            }
            else
            {
                return 0; //vec gadjano polje
            }
            //server ce na osnovu povratne vrednosti da ispise poruku protivniku
        }
        public string PrikaziMatricu()
        {
            string s="\t";
            for (int i = 0; i < matrica.GetLength(0); i++)
            {
                for (int j = 0; j < matrica.GetLength(1); j++)
                {

                    s = s + matrica[i, j] + " ";
                }
                s = s + "\n\t"; // Novi red posle svake vrste
            }
            return s;
        }
        public override string ToString()
        {
            string s = $"----------\nIgrac ID={id} Ime={ime}\n----------\nBroj promasaja: {brojPromasaja}";
            s = s + $"\nTabla:\n{PrikaziMatricu()}\n\n";
            return s;
        }

        public  byte[] SerijalizujMatricu()
        {
            byte[] serializedMatrix;
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, matrica);
                serializedMatrix = ms.ToArray(); 
            }
            return serializedMatrix;
        }
    }
}
