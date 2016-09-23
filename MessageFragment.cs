
using TEArts.Etc.CollectionLibrary;

namespace TEArts.Networking.AsyncSocketer
{
    public class MessageFragment : IDentity
    {
        public byte[] Buffer { get; set; }
        public int IDentity { get; set; }
        public override string ToString()
        {
            return string.Format(" IDentity : {0} , Value : {1}{2}", IDentity, System.Environment.NewLine, BiteArray.FormatArrayMatrix(Buffer));
        }
    }
}
