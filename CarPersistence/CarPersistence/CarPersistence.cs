using GTA;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace CarPersistence
{
    /* Goals:
     *                             WORKING | WORKING | WORKING
     *   1. Track driven vehicles (New | already known | respect limit)
     *   
     *                                   WORKING
     *   2. Track saved vehicles (Transfer between driven and saved)
     *
     *   3. Blips (Different Color for driven/saved)
     */

    /* Notes on functionality:
     * 
     * 1.   Saved vehicles will be treated as driven vehicles. That means in a session you can take out
     *      any of your saved cars drive around and it will only reset itself to the saved-position on a reload.
     * 
     * 2.   
     */ 

    public class CarPersistence : Script
    {
        // Util
        private Stopwatch stopwatch = new Stopwatch();

        private long cleanUpTime = 0;
        private long blipTime = 0;
        private long generalTime = 0;

        // Easy availability
        private Ped player;
        // Limit how many vehicles we want to remember the player last entered
        private int drivenVehiclesLimit = 3;

        // drivenVehicles contains all vehicles that are currently driven
        private Dictionary<int, VehicleData> drivenVehicles = new Dictionary<int, VehicleData>();
        // contains all vehicles that are saved but not currently driven
        private Dictionary<int, VehicleData> savedVehicles = new Dictionary<int, VehicleData>();

        public CarPersistence()
        {
            onLoad();

            // delegates [tick comes from script and OnAbort forexample gets called...]
            Tick += OnTick;
            Aborted += OnAbort;

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            Interval = 50;
        }

        // Executes Logic that is needed on script-start
        private void onLoad()
        {
            // Get player for easier access
            player = Game.Player.Character;
        }

        // The actions that are perdormed on every tick
        void OnTick(object sender, EventArgs e)
        {
            // If limit is exceeded oldest vehicles will be removed
            stopwatch.Restart();

            cleanUpDrivenVehicles();

            stopwatch.Stop();
            cleanUpTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            updateBlips();

            stopwatch.Stop();
            blipTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            // Logic to check if player is in a vehicle and if this vehicle is known to the system
            if(player.IsInVehicle())
            {
                Vehicle v = player.CurrentVehicle;
                int vHandle = v.Handle;

                // Check if the vehicles is marked as driven
                if(drivenVehicles.ContainsKey(vHandle))
                {
                    VehicleData tmp;
                    drivenVehicles.TryGetValue(vHandle, out tmp);
                    tmp.updateTimestamp();
                }
                // Check if the vehicle is saved
                else if (savedVehicles.ContainsKey(vHandle))
                {
                    changeToDriven(v);
                }
                else
                {
                    // If vehicle is a new vehicle
                    // TODO add vehicle to driven list
                    changeToDriven(v);
                }
            }

            stopwatch.Stop();
            generalTime = stopwatch.ElapsedMilliseconds;
        }

        // When the script is aborted/reloaded
        void OnAbort(object sender, EventArgs e)
        {
            // MAKE SURE TO DELETE EVERY VEHICLE WE HAVE A HANDLE TO SO THERE APPEAR NO DUPLICATES ON RELOAD
            clearBlips();
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
        }
        
        void OnKeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.J)
            {
                toggleSaveState(player.CurrentVehicle);
            }

            if (e.KeyCode == Keys.K)
            {
                string vehicles = "";
                foreach (KeyValuePair<int, VehicleData> entry in drivenVehicles)
                {
                    vehicles += entry.Value.name;
                    vehicles += ";";
                }
                UI.Notify(vehicles);
            }

 /*           if (e.KeyCode == Keys.U)
            {
                UI.Notify("" + player.CurrentVehicle.Handle);
            }
*/
            if (e.KeyCode == Keys.U)
            {
                UI.Notify("CUT: " + cleanUpTime + "\nBT:  " + blipTime + "\nGT:  " + generalTime);
            }
        }

        /* Will look at all vehicles marked as currently driven
         * and will remove the oldest vehicles if there are more
         * vehicles than the set limit (First in first out)
         * HINT: If a player stays in a vehicle or reenters one
         * the timestamp will be reset. 
        */
        private void cleanUpDrivenVehicles()
        {
            // Cleans up as long as there are more driven vehicles than allowed
            while (drivenVehicles.Count > drivenVehiclesLimit)
            {
                long oldestTime = getTimestamp();
                int oldestKey =0;

                // Finds the oldest vehicle
                foreach (KeyValuePair<int, VehicleData> entry in drivenVehicles)
                {
                    if (entry.Value.timeStamp < oldestTime)
                    {
                        oldestTime = entry.Value.timeStamp;
                        oldestKey = entry.Key;
                    }
                }

                // Deletes the oldest vehicle or moves it to saved list
                if(oldestKey != 0)
                {
                    changeToNoLongerDriven(oldestKey);
                }
            }
        }

        // Moves a vehicle to driven list
        private void changeToDriven(Vehicle v)
        {
            // Handle saved vehicles
            if (savedVehicles.ContainsKey(v.Handle))
            {
                // If vehicle is saved it is moved to driven list
                VehicleData tmp;
                savedVehicles.TryGetValue(v.Handle, out tmp);
                tmp.timeStamp = getTimestamp();
                drivenVehicles.Add(v.Handle, tmp);
                savedVehicles.Remove(v.Handle);
                v.IsPersistent = true;
                UI.Notify("Moved saved vehicle [S -> D][" + v.DisplayName + "]");
            }
            // Handle new vehicles
            else
            {
                // New vehicle is added to driven list
                drivenVehicles.Add(v.Handle, new VehicleData(v, 0, getTimestamp(), v.Model.Hash, v.DisplayName));
                v.IsPersistent = true;
                UI.Notify("Added unsaved Vehicle [" + v.DisplayName + "](" + drivenVehicles.Count + ")");
            }
        }

        // Removes a vehicle from driven list
        private void changeToNoLongerDriven(int handle)
        {

            VehicleData tmp;
            drivenVehicles.TryGetValue(handle, out tmp);

            if(!tmp.saved)
            {
                // Find vehicle in world
                foreach (Vehicle ve in World.GetAllVehicles())
                {
                    if (ve.Handle == handle)
                    {
                        // Handle unsaved vehicles
                        ve.IsPersistent = false;
                        VehicleData blipdel = null;
                        drivenVehicles.TryGetValue(handle, out tmp);
                        blipdel.b.Remove();
                        UI.Notify("Removed unsaved Vehicle [" + ve.DisplayName + "]");
                        break;
                    }
                }
                drivenVehicles.Remove(handle);
            } else
            {
                // If the vehicle is saved the persistent status doesn't need to be adjusted
                savedVehicles.Add(handle, tmp);
                drivenVehicles.Remove(handle);
                UI.Notify("Moved saved Vehicle [D -> S][" + tmp.name + "]");
            }

        }

        // Marks the vehicle the player is in as saved
        private void toggleSaveState(Vehicle v)
        {
            if (drivenVehicles.ContainsKey(v.Handle))
            {
                VehicleData tmp;
                drivenVehicles.TryGetValue(v.Handle, out tmp);
                tmp.saved = !tmp.saved;
                if(tmp.saved)
                {
                    UI.Notify("Vehicle saved [" + v.DisplayName + "]");
                } else
                {
                    UI.Notify("Vehicle unsaved [" + v.DisplayName + "]");
                }
            }
        }

        private void updateBlips()
        {

            Blip b = null;
            foreach (KeyValuePair<int, VehicleData> entry in drivenVehicles)
            {
                if (entry.Value.b == null)
                {
                    b = World.CreateBlip(entry.Value.v.Position);
                    entry.Value.b = b;
                    b.Color = BlipColor.Red;
                    //b.Sprite = BlipSprite.PersonalVehicleCar;
                    b.Scale = 0.8f;
                    // FIND OUT HOW TO DO COLORED VEHICLE SPRITES
                    //b.Color = BlipColor.Red;
                }
                entry.Value.b.Position = entry.Value.v.Position;
            }
            foreach (KeyValuePair<int, VehicleData> entry in savedVehicles)
            {
                if (entry.Value.b == null)
                {
                    b = World.CreateBlip(entry.Value.v.Position);
                    entry.Value.b = b;
                    b.Color = BlipColor.Green;
                    //b.Sprite = BlipSprite.PersonalVehicleCar;
                    b.Scale = 0.8f;
                }
                entry.Value.b.Position = entry.Value.v.Position;
            }

        }

        private void clearBlips()
        {
            foreach (KeyValuePair<int, VehicleData> entry in drivenVehicles)
            {
                if (entry.Value.b != null)
                {
                    entry.Value.b.Remove();
                }
            }
            foreach (KeyValuePair<int, VehicleData> entry in savedVehicles)
            {
                if (entry.Value.b != null)
                {
                    entry.Value.b.Remove();
                }
            }
        }

        // this may cause bad performance if called from inside a loop
        private Vehicle findVehicleByHandle(int handle)
        {
            foreach(Vehicle v in World.GetAllVehicles())
            {
                if(handle == v.Handle)
                {
                    return v;
                }
            }
            return null;
        }

        public static long getTimestamp()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }
    }

 
}
