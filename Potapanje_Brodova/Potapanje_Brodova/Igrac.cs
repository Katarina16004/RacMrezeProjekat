using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Potapanje_Brodova
{
    public class Igrac
    {
        public int id { get; set; }
        public int brojPromasaja { get; set; }
        public List<int> pozicije { get; set; } //korisnik salje pozicije (1-dim)
        public int[,] matrica { get; set; }

        public Igrac(int id, int dimenzija)
        {
            this.id = id;
            brojPromasaja = 0;
            pozicije = new List<int>();
            matrica = new int[dimenzija, dimenzija];
        }
        public void DodajPodmornice(List<int> pozicije)
        {
            this.pozicije = pozicije;
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
    }
}
