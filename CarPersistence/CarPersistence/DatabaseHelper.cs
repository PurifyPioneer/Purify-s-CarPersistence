using System.Data.SQLite;
using System.Collections;
using System.Collections.Generic;

namespace CarPersistence
{
    class DatabaseHelper
    {
        //
        SQLiteConnection m_dbConnection;

        public DatabaseHelper()
        {

            SQLiteConnection.CreateFile("MyDatabase.sqlite");
                        
            m_dbConnection = new SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3;");
            m_dbConnection.Open();
        }

        void createDatabase()
        {
            // create database on first start or if not found
        }

        void loadDatabase()
        {
            // Connect to database
        }

        ArrayList loadVehicles()
        {
            ArrayList vehicles = new ArrayList();

            string sql = "SELECT * FROM ....";
            SQLiteCommand cmnd = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = cmnd.ExecuteReader();

            while(reader.Read())
            {
                // TODO populate array list
            }


            return vehicles;
        }

        void saveVehicles(Dictionary<int, VehicleData> vehicles)
        {

            foreach (KeyValuePair<int, VehicleData> v in vehicles)
            {
                // TODO IF ID EXISTS IN DB
                // ELSE INSERT
                string sql = "INSERT INTO vehicles .... VALUES";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.ExecuteNonQuery();

            }


        }
        {

        }

    }
}
