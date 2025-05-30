using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class Poruka
    {
        public Igrac NaPotezu { get; set; }
        public Igrac Napadnut { get; set; }
        public TipPoruke tipPoruke { get; set; }
        public string poruka { get; set; }

        public Poruka(Igrac naPotezu, Igrac napadnut, TipPoruke tipPoruke, string poruka)
        {
            NaPotezu = naPotezu;
            Napadnut = napadnut;
            this.tipPoruke = tipPoruke;
            this.poruka = poruka;
        }

        public Poruka()
        {

        }

        public byte[] Serializuj()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, this);
                return ms.ToArray();
            }
        }

        public Poruka DeserializujPoruku(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                BinaryFormatter bf = new BinaryFormatter();
                Poruka objekat = (Poruka)bf.Deserialize(ms);
                return objekat;
            }
        }

    }
}
