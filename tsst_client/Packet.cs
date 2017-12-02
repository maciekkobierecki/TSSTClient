using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tsst_client;

namespace tsst_client
{
    [Serializable]
    public class Packet
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

        public Packet(Packet copyingPacket)
        {
            s = copyingPacket.s;
            destinationPort = copyingPacket.destinationPort;
            sourcePort = copyingPacket.sourcePort;
            destinationAddress = copyingPacket.destinationAddress;
            sourceAddress = copyingPacket.sourceAddress;
            timestamp = copyingPacket.timestamp;
            _interface = copyingPacket._interface;
        }



    }
}
