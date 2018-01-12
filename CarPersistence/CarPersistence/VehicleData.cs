using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarPersistence
{
    class VehicleData
    {
        public Vehicle v { get; set; }
        public Blip b { get; set; }
        private int ID;
        public bool saved { get; set; }
        public long timeStamp { get; set; }
        private int modelHash;
        public string name { get; set; }

        public VehicleData(Vehicle v, int ID, long timeStamp, int modelHash, string name)
        {
            this.v = v;
            this.ID = ID;
            this.timeStamp = timeStamp;
            this.modelHash = modelHash;
            this.name = name;
        }

        public void updateTimestamp()
        {
            this.timeStamp = CarPersistence.getTimestamp();
        }
    }
}
