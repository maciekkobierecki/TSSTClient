using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tsst_client
{
    [Serializable]
    class Packet
    {
        public string s;
        public int destinationPort;
        public int sourcePort;
        public String destinationAddress;
        public String sourceAddress;
        public string timestamp;
        public string _interface;

        public Packet(string packetBody, String destinationAddress, String sourceAddress,
            int destinationPort, int sourcePort, string time, string _in)
        {
            this.s = packetBody;
            this.destinationAddress = destinationAddress;
            this.sourceAddress = sourceAddress;
            this.destinationPort = destinationPort;
            this.sourcePort = sourcePort;
            this.timestamp = time;
            this._interface = _in;
        }



    }
}
