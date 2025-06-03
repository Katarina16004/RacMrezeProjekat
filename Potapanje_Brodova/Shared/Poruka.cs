using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Shared
{
    [Serializable]
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

        public static Poruka DeserializujPoruku(byte[] bytes)
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
