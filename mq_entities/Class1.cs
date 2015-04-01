using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace mq_entities
{
    [DataContract]
    public class Person
    {
        [DataMember]
        public string name;

        [DataMember]
        public int age;

        [DataMember]
        public byte[] picture;

        public Person()
        {
            name = "name";
            age = 0;
        }

        public void SetPicture(Image picture)
        {
            this.picture = imageToByteArray(picture);
        }

        public Image GetPicture()
        {
            return this.byteArrayToImage(this.picture);
        }

        private byte[] imageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            return ms.ToArray();
        }

        private Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            Image returnImage = Image.FromStream(ms);
            return returnImage;
        }
    }

    public class Transport
    {
        public Transport(Person person)
        {
            this.Person = person;
        }

        public Person Person { get; set; }

        public string GetJson()
        {
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Person));
            return ser.ToString();
        }
    }
}
