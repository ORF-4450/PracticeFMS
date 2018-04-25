using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PFMS
{
    class DriverStationStatusPacket
    {
        public enum PacketType { KEEP_ALIVE, ROBOT_CONTROL, BAD_PACKET };

        static DriverStationStatusPacket decodeDriverStationStatusPacket(byte[] data)
        {
            DriverStationStatusPacket packet = new DriverStationStatusPacket();
            if ((int)data[2] == 28)
            {
                packet.packetType = PacketType.KEEP_ALIVE;
                return packet;
            }
            else if ((int)data[2] != 22)
            {
                packet.packetType = PacketType.BAD_PACKET;
                return packet;
            }

            packet.packetType = PacketType.ROBOT_CONTROL;

            return packet;
        }

        public DriverStationStatusPacket()
        {

        }

        public PacketType packetType;
    }
}
