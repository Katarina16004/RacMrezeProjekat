using System.Net;

namespace Server
{
    public class Klijent
    {
        public string Ime { get; set; }
        EndPoint  IPAdresa { get; set; }

        public Klijent(string ime, EndPoint adresa)
        {
            Ime = ime;
            IPAdresa = adresa;
        }

        public override string ToString()
        {
            return "\n" + Ime + " " + IPAdresa.ToString();
        }
    }
}