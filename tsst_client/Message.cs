using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tsst_client
{
    [Serializable]
    public class Message
    {
        public string s;
        public int input_port;
        public int output_port;
        public string timestamp;
        public string _interface;
      
        public Message(string str, int i, int p, string time, string _in)
        {
            this.s = str;
            this.input_port = i;
            this.output_port = p;
            this.timestamp = time;
            this._interface = _in;
        }

        public Message()
        {
        }

    }
}
