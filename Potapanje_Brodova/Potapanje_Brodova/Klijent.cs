using System.IO;
using System;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

namespace Server
{
    public class Klijent
    {
        public string Ime { get; set; }
        public  EndPoint  IPAdresa { get; set; }

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