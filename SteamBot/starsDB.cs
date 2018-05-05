using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//Add MySql Library
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using TradeAsset = SteamTrade.TradeOffer.TradeOffer.TradeStatusUser.TradeAsset;
using SteamKit2;
using SteamTrade;
using SteamTrade.TradeOffer;
using System.Globalization;
using System.Timers;
using System.Collections;

using System.Net;
using System.Net.Mail;
using System.Net.Mime;




namespace SteamBot
{
    class starsDB
    {
        //mysql config
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;

        private dynamic result;

        private readonly GenericInventory mySteamInventory;
        private readonly GenericInventory OtherSteamInventory;

        public starsDB()
        {
            Initialize();
        }
        //initialize the database information so we can establbish a connection when need be
        private void Initialize()
        {
            server = "csgostars.com";
            database = "csgostarsDB";
            uid = "csgostarsDB";
            password = "0mh-dkm,=vF2";
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
        }

        //open connection to database
        private bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        Console.WriteLine("Cannot connect to server.  Contact administrator");
                        break;

                    case 1045:
                        Console.WriteLine("Invalid username/password, please try again");
                        break;
                }
                return false;
            }
        }

        //Close connection
        private bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /**
        *UnixTimeNow function outputs the time in unix standard at 1970,1,1,0,0,0 
        *@return long currentTime
        */
        public long UnixTimeNow()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        //submitBet submits a bet to the stars db for approval by the user who sent a trade 
        public bool submitBet(string _botID, string _steamID, string _tradeID, double _valueTotal, int _numberOfItems)
        {
            try
            {
                //grab the current time for time stamping transaction
                long currentTime = this.UnixTimeNow();
                string query = "INSERT INTO betsSubmitted (botID, steamID, tradeID, valueTotal, numberOfItems, offerSentTime, approved, requestSent) VALUES('" + _botID + "', '" + _steamID + "', '" + _tradeID + "', '" + _valueTotal + "', '" + _numberOfItems + "', '" + currentTime + "', 'False', 'False')";

                //open connection
                if (this.OpenConnection() == true)
                {
                    //create command and assign the query and connection from the constructor
                    MySqlCommand cmd = new MySqlCommand(query, connection);

                    //Execute command
                    cmd.ExecuteNonQuery();

                    //close connection
                    this.CloseConnection();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        //checkIfBetAlreadySubmitted function checks if the tradeID is already listed to avoid duplicates from being created
        public bool checkIfBetAlreadySubmitted(string tradeID)
        {
            try
            {
                string query = "SELECT * FROM betsSubmitted WHERE tradeID='" + tradeID + "'";
                string currTradeID = "";
                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //Read the data and store them in the list
                    while (dataReader.Read())
                    {
                        currTradeID = (dataReader["tradeID"] + "");
                        break;
                    }
                    //close Data Reader
                    dataReader.Close();

                    //close Connection
                    this.CloseConnection();
                }
                if (currTradeID.Equals(""))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }


        }

        //addBetQueItem adds an item to the bet que 
        public bool addBetQueItem(string botID, string steamID, string tradeID, string descriptionID, string marketName, double marketValue)
        {
            //generate a random hash id for specific item
            string hashID = this.getUniqueHashID();

            try
            {
                string imageURL = "";
                string nameColour = "";

                getItemDescription(steamID, descriptionID, out imageURL, out nameColour);

                string query = "INSERT INTO betQueItems (botID, steamID, tradeID, descriptionID, marketName, valueOfItem, hashID, itemColour, imageURL) VALUES('" + botID + "', '" + steamID + "', '" + tradeID + "', '" + descriptionID + "','" + marketName + "','" + marketValue + "','" + hashID + "', '" + nameColour + "', '" + imageURL + "')";

                //open connection
                if (this.OpenConnection() == true)
                {
                    //create command and assign the query and connection from the constructor
                    MySqlCommand cmd = new MySqlCommand(query, connection);

                    //Execute command
                    cmd.ExecuteNonQuery();

                    //close connection
                    this.CloseConnection();
                }
                return true;
            }
            catch
            {
                //failed case
                return false;
            }
        }

        //getItemDescription gets the rest of the description assets required for the item 
        public void getItemDescription(String steamID, String descriptionID, out string image, out string nameColour)
        {
            string imageTemp = "";
            string nameColourTemp = "";
            try
            {
                using (var webClient = new System.Net.WebClient())
                {
                    if (result == null)
                    {
                        String url = "http://steamcommunity.com/profiles/" + steamID + "/inventory/json/730/2";
                        result = JsonConvert.DeserializeObject(webClient.DownloadString(url));
                    }
                    imageTemp = result.rgDescriptions[descriptionID].icon_url;
                    nameColourTemp = result.rgDescriptions[descriptionID].name_color;
                }
                image = imageTemp;
                nameColour = nameColourTemp;
            }
            catch
            {
                //return blanks if we can't acccess the api to call the image url and item colour
                image = "";
                nameColour = "";
            }

        }

        //getUniqueHashID makes a random hash id for assigning to items so we can keep track of them from one state to another 
        public string getUniqueHashID()
        {
            Guid guid = Guid.NewGuid();
            string str = guid.ToString();
            return str;
        }

        //getNextTradeID grabs the next avail. tradeID for the bot to accept and out puts that info 
        public bool getNextTradeID(string myBotID, out string traderSID, out string tradeID)
        {
            //set default variables
            bool tradeFound = false;
            tradeID = "";
            traderSID = "";
            try
            {
                //build query
                string query = "SELECT * FROM betQue WHERE botID='" + myBotID + "' ORDER BY offerSentTime";

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //Read only the first tradeID and steamID in the datareader and break
                    while (dataReader.Read())
                    {
                        tradeID = (dataReader["tradeID"] + "");
                        traderSID = (dataReader["steamID"] + "");
                        tradeFound = true;
                        break;
                    }

                    //close Data Reader
                    dataReader.Close();

                    //close Connection
                    this.CloseConnection();
                }
            }
            catch
            {
                //return false if we fail to connect to DB
                return false;
            }

            return tradeFound;
        }
        //deleteInvalidTradeOffer remove the specified tradeID from que  
        public bool deleteInvalidTradeOffer(string tradeID)
        {
            try
            {
                if (this.OpenConnection() == true)
                {
                    //remove from que
                    string query = "DELETE FROM betsSubmitted WHERE tradeID='" + tradeID + "'";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.ExecuteNonQuery();

                    //delete from betQueItems List
                    query = "DELETE FROM betQueItems WHERE tradeID='" + tradeID + "'";
                    cmd = new MySqlCommand(query, connection);
                    cmd.ExecuteNonQuery();

                    //send notification
                    query = "UPDATE depositNotification SET status='Invalid Offer' WHERE tradeID='" + tradeID + "'";
                    cmd = new MySqlCommand(query, connection);
                    cmd.ExecuteNonQuery();

                    this.CloseConnection();
                    return true;
                }
            }
            catch
            {
            }
            //failed case return false
            return false;
        }

        //getCurrentLotto
        public string getCurrentLotto()
        {
            string lottoID = "";
            try
            {
                string query = "SELECT * FROM liveLotterySummary";

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //Read the data and store them in the list
                    while (dataReader.Read())
                    {
                        lottoID = (dataReader["lotteryID"] + "");
                        break;
                    }

                    //close Data Reader
                    dataReader.Close();

                    //close Connection
                    this.CloseConnection();
                }
            }
            catch
            {

            }
            return lottoID;
        }
        //updateLottoStats adds the values to the total of the current lottoID
        public bool updateLottoStats(int itemCount, double valueOfItems, string lotteryID)
        {
            int currentNumberOfItems = 0;
            double currentTotalValue = 0;
            try
            {
                string query = "SELECT * FROM liveLotterySummary";

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    // grab current values
                    while (dataReader.Read())
                    {
                        currentNumberOfItems = Convert.ToInt16(dataReader["numberOfItems"] + "");
                        currentTotalValue = Convert.ToDouble(dataReader["totalValueOfPot"] + "");
                        break;
                    }

                    //close Data Reader
                    dataReader.Close();
                    //increment counters
                    currentNumberOfItems = currentNumberOfItems + itemCount;
                    currentTotalValue = currentTotalValue + valueOfItems;

                    //submit update
                    string updateQ = "UPDATE liveLotterySummary SET numberOfItems='" + currentNumberOfItems + "', totalValueOfPot='" + currentTotalValue + "' WHERE lotteryID='" + lotteryID + "'";
                    //send command to update stats
                    cmd = new MySqlCommand(updateQ, connection);
                    cmd.ExecuteNonQuery();

                    //close Connection
                    this.CloseConnection();
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        //addToBetInventory (the bot inventory list) 
        public bool addToBetInventory(string tradeID)
        {
            //grab the current lottoID
            string lotteryID = getCurrentLotto();
            Console.WriteLine(lotteryID);
            //counters 
            int itemCount = 0;
            double valueOfItems = 0;
            try
            {
                string query = "SELECT * FROM betQueItems WHERE tradeID='" + tradeID + "'";
                Console.WriteLine(query);
                //Create a list to store the result
                List<string>[] list = new List<string>[9];
                list[0] = new List<string>();
                list[1] = new List<string>();
                list[2] = new List<string>();
                list[3] = new List<string>();
                list[4] = new List<string>();
                list[5] = new List<string>();
                list[6] = new List<string>();
                list[7] = new List<string>();
                list[8] = new List<string>();

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //Read the data and store them in the list
                    while (dataReader.Read())
                    {
                        Console.WriteLine("here");
                        list[0].Add(dataReader["botID"] + "");
                        list[1].Add(dataReader["steamID"] + "");
                        list[2].Add(dataReader["tradeID"] + "");
                        list[3].Add(dataReader["descriptionID"] + "");
                        list[4].Add(dataReader["marketName"] + "");
                        list[5].Add(dataReader["valueOfItem"] + "");
                        list[6].Add(dataReader["hashID"] + "");
                        list[7].Add(dataReader["itemColour"] + "");
                        list[8].Add(dataReader["imageURL"] + "");

                        valueOfItems = valueOfItems + Convert.ToDouble(dataReader["valueOfItem"]);
                        itemCount = itemCount + 1;
                    }
                    //close Data Reader
                    dataReader.Close();

                    long currentTime = UnixTimeNow();
                    //insert all items that are traded into betInventory
                    for (int i = 0; i < list[0].Count; i++)
                    {
                        string insertQ = "INSERT INTO betInventory (botID, steamID, tradeID, imageURL, descriptionID, colour, marketName, ValueOfItem, timeDeposited, lotteryID, hashID, updatedList) VALUES('" + list[0][i] + "', '" + list[1][i] + "', '" + list[2][i] + "', '" + list[8][i] + "', '" + list[3][i] + "', '" + list[7][i] + "', '" + list[4][i] + "', '" + list[5][i] + "', '" + currentTime + "','" + lotteryID + "', '" + list[6][i] + "','False')";
                        //create command and assign the query and connection from the constructor
                        cmd = new MySqlCommand(insertQ, connection);
                        cmd.ExecuteNonQuery();
                    }
                    //move to history
                    string insQ = "INSERT INTO betQue_History select * FROM betQue WHERE tradeID='" + tradeID + "'";
                    cmd = new MySqlCommand(insQ, connection);
                    cmd.ExecuteNonQuery();
                    //remove from que
                    string delQ = "DELETE FROM betQue WHERE tradeID='" + tradeID + "'";
                    cmd = new MySqlCommand(delQ, connection);
                    cmd.ExecuteNonQuery();
                    //delete items from betQueList
                    string deleteQ = "DELETE FROM betQueItems WHERE tradeID='" + tradeID + "'";
                    cmd = new MySqlCommand(deleteQ, connection);
                    cmd.ExecuteNonQuery();

                    //close Connection
                    this.CloseConnection();
                    updateLottoStats(itemCount, valueOfItems, lotteryID);
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        //check lotto status
        public bool lottoStatus()
        {
            bool lottoStatus = true;
            try
            {
                string query = "SELECT * FROM liveLotterySummary";

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //Read the data and store them in the list
                    while (dataReader.Read())
                    {
                        lottoStatus = Convert.ToBoolean(dataReader["ended"] + "");
                    }

                    //close Data Reader
                    dataReader.Close();

                    //close Connection
                    this.CloseConnection();
                }
            }
            catch
            {

            }
            return lottoStatus;
        }

        public string getDistroBotID()
        {
            //default bot
            return "76561198126391561";
        }

        public string getDistroTradeToken()
        {
            //distro bot trade token
            return "g3yyE3mi";
        }

        public List<string>[] getDistributionList(string botID, string lotteryID)
        {
            List<string>[] list = new List<string>[12];
            list[0] = new List<string>();
            list[1] = new List<string>();
            list[2] = new List<string>();
            list[3] = new List<string>();
            list[4] = new List<string>();
            list[5] = new List<string>();
            list[6] = new List<string>();
            list[7] = new List<string>();
            list[8] = new List<string>();
            list[9] = new List<string>();
            list[10] = new List<string>();
            list[11] = new List<string>();
            try
            {
                string query = "SELECT * FROM betInventory WHERE botID='" + botID + "' AND lotteryID='" + lotteryID + "'";

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //Read the data and store them in the list
                    while (dataReader.Read())
                    {
                        list[0].Add(dataReader["botID"] + "");
                        list[1].Add(dataReader["steamID"] + "");
                        list[2].Add(dataReader["tradeID"] + "");
                        list[3].Add(dataReader["imageURL"] + "");
                        list[4].Add(dataReader["descriptionID"] + "");
                        list[5].Add(dataReader["colour"] + "");
                        list[6].Add(dataReader["marketName"] + "");
                        list[7].Add(dataReader["valueOfItem"] + "");
                        list[8].Add(dataReader["timeDeposited"] + "");
                        list[9].Add(dataReader["lotteryID"] + "");
                        list[10].Add(dataReader["hashID"] + "");
                        list[11].Add(dataReader["updatedList"] + "");
                    }

                    //close Data Reader
                    dataReader.Close();

                    //close Connection
                    this.CloseConnection();

                    //return list to be displayed
                    return list;
                }
            }
            catch
            {

            }
            return list;
        }
        //updateDistributionStatus changes the status of the updated list 
        public bool updateDistributionStatus(List<string>[] distributionList)
        {
            try
            {
                //Open connection
                if (this.OpenConnection() == true)
                {
                    for (int i = 0; i < distributionList[0].Count; i++)
                    {
                        if (distributionList[11][i].Equals("True"))
                        {
                            string query = "UPDATE betInventory SET updatedList='True' WHERE hashID='" + distributionList[10][i] + "'";
                            //create mysql command
                            MySqlCommand cmd = new MySqlCommand();
                            //Assign the query using CommandText
                            cmd.CommandText = query;
                            //Assign the connection using Connection
                            cmd.Connection = connection;

                            //Execute query
                            cmd.ExecuteNonQuery();
                        }
                    }
                    //close connection
                    this.CloseConnection();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        //moveToDistroSent moves all betinventory items being sent to distro
        public bool moveToDistroSent(List<string>[] distributionList, string distroTradeID)
        {
            try
            {
                //Open connection
                if (this.OpenConnection() == true)
                {
                    for (int i = 0; i < distributionList[0].Count; i++)
                    {
                        if (distributionList[11][i].Equals("True"))
                        {
                            string query = "INSERT INTO sendToDistribution (botID, steamID,tradeID, imageURL, descriptionID, colour, marketName, valueOfItem, timeDeposited, lotteryID, hashID) SELECT botID, steamID, tradeID, imageURL, descriptionID, colour, marketName, valueOfItem, timeDeposited, lotteryID, hashID FROM betInventory WHERE hashID='" + distributionList[10][i] + "'";
                            //create mysql command
                            MySqlCommand cmd = new MySqlCommand();
                            //Assign the query using CommandText
                            cmd.CommandText = query;
                            //Assign the connection using Connection
                            cmd.Connection = connection;

                            //Execute query
                            cmd.ExecuteNonQuery();

                            string updateQ = "UPDATE sendToDistribution SET itemSent='True', toDisTradeID='" + distroTradeID + "' WHERE hashID='" + distributionList[10][i] + "'";
                            cmd = new MySqlCommand(updateQ, connection);
                            cmd.ExecuteNonQuery();

                            string deleteQ = "DELETE FROM betInventory WHERE hashID='" + distributionList[10][i] + "'";
                            cmd = new MySqlCommand(deleteQ, connection);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    //close connection
                    this.CloseConnection();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool distroRecievedTrade(string tradeID)
        {
            string query = "INSERT INTO distributionList (botID, steamID,tradeID, imageURL, descriptionID, colour, marketName, valueOfItem, timeDeposited, lotteryID, hashID, toDisTradeID) SELECT botID, steamID, tradeID, imageURL, descriptionID, colour, marketName, valueOfItem, timeDeposited, lotteryID, hashID, toDisTradeID FROM sendToDistribution WHERE toDisTradeID='" + tradeID + "'";
            try
            {
                //open connection
                if (this.OpenConnection() == true)
                {
                    Console.WriteLine(query);
                    //create command and assign the query and connection from the constructor
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.ExecuteNonQuery();

                    //update the newOwner to default 0000
                    query = "UPDATE distributionList SET newOwnerID='0000', itemID='0000' WHERE toDisTradeID='"+ tradeID +"'";
                    cmd = new MySqlCommand(query, connection);
                    cmd.ExecuteNonQuery();

                    query = "DELETE FROM sendToDistribution WHERE toDisTradeID='"+ tradeID + "'";
                    cmd = new MySqlCommand(query, connection);
                    cmd.ExecuteNonQuery();

                    //close connection
                    this.CloseConnection();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool getLottoTotal(string lotteryID, out double totalValue, out int numberOfItems)
        {
            bool complete = false;
            totalValue = -1;
            numberOfItems = 0;
            try
            {
                string query = "SELECT * FROM liveLotterySummary";

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //Read the data and store them in the list
                    while (dataReader.Read())
                    {
                        complete = true;
                        numberOfItems = Convert.ToInt16(dataReader["numberOfItems"] + "");
                        totalValue = Convert.ToDouble(dataReader["totalValueOfPot"] + "");
                    }
                    //close Data Reader
                    dataReader.Close();

                    //close Connection
                    this.CloseConnection();
                    return complete;
                }
            }
            catch
            {
            }
            return complete;
        }

        public bool checkIfCollectionComplete(string lotteryID, int totalNumberOfItems, double totalValueOfItems)
        {
            try
            {
                string query = "SELECT * FROM  distributionList WHERE lotteryID='" + lotteryID + "'";
                double valueOfItems = 0;
                int numberOfItems = 0;

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //Read the data and store them in the list
                    while (dataReader.Read())
                    {
                        valueOfItems = valueOfItems + Convert.ToDouble(dataReader["valueOfItem"] + "");
                        numberOfItems = numberOfItems + 1;
                    }

                    //close Data Reader
                    dataReader.Close();

                    //fix this shit decimal place bullshit
                    //close Connection
                    this.CloseConnection();
                    Console.WriteLine("debug collection complete " + valueOfItems + " " + numberOfItems);
                    Console.WriteLine("debug collection complete totes " + totalValueOfItems + " " + totalNumberOfItems);
                    if (Convert.ToInt16(valueOfItems*100) == Convert.ToInt16(totalValueOfItems*100) && numberOfItems == totalNumberOfItems) 
                    {
                        Console.WriteLine("test");
                        return true;
                    }
                }
            }
            catch
            {

            }
            return false;
        }

        public string decideLotteryWinner(string lotteryID)
        {
            List<string> steamIDs = new List<string>();
            List<double> valueOfSID = new List<double>();
            double totalPot = 0;
            try 
            {
                string query = "SELECT * FROM distributionList WHERE lotteryID='"+ lotteryID +"'";

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();
        
                    //Read the data and store them in the list
                    while (dataReader.Read())
                    {
                        string currentSID = (dataReader["steamID"] + "");
                        double currentValue = Convert.ToDouble(dataReader["valueOfItem"] + "");
                        bool found = false;
                        //search existing list of players to see if the player is already listed
                        for(int i=0; i<steamIDs.Count; i++) {
                            //found the steamID already exists
                            if(currentSID.Equals(steamIDs[i])) {
                                found = true;
                                //sum to value to increase chance of winning
                                valueOfSID[i] = valueOfSID[i] + currentValue;
                            }
                        }
                        //in the case where the steam id hasn't been added to the list yet
                        if(found == false) {
                            steamIDs.Add(currentSID);
                            valueOfSID.Add(currentValue);
                        }
                        totalPot = totalPot + currentValue;
                        Console.WriteLine(totalPot);
                    }
                    //close Data Reader
                    dataReader.Close();

                    //close Connection
                    this.CloseConnection();
                }
                
                //create a random number between the total and 0 
                Random r = new Random();
                long winnerValue = r.Next(0,Convert.ToInt16(totalPot * 100));
                //keep incrementing till we find the winner!
                int total = 0;
                for(int i=0; i<steamIDs.Count;i++) {
                    total = (total + Convert.ToInt16(valueOfSID[i] * 100));
                    if(winnerValue <= total) {
                        //found the winner 
                        return steamIDs[i];
                    }
                }
            } catch {

            }
            return "0000";
        }

        public bool getPercentCut(out double percentageCut)
        {
            string query = "SELECT * FROM lotterySettings";
            percentageCut = 0;
            try
            {
                //Create a list to store the result
                List< string >[] list = new List< string >[3];
                list[0] = new List< string >();
                list[1] = new List< string >();
                list[2] = new List< string >();

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //Read the data and store them in the list
                    while (dataReader.Read())
                    {
                        percentageCut = Convert.ToDouble(dataReader["percentageCut"] + "");
                        break;
                    }

                    //close Data Reader
                    dataReader.Close();

                    //close Connection
                    this.CloseConnection();
                }
                return true;
            }
            catch
            {

            }
            return false;
        }

        public List<string> buildCutList(string lotteryID, double cutPercentage, double totalPot)
        {
            string query = "SELECT * FROM distributionList WHERE lotteryID='" + lotteryID + "' ORDER BY valueOfItem ASC";
            //Create a list to store the result
            List<string>[] list = new List<string>[2];
            //hashID
            list[0] = new List<string>();
            //valueOfItem
            list[1] = new List<string>();
            
            double cutValue = cutPercentage * totalPot;

            //list of hashIDs that are part of our value cut 
            List<string> cutList = new List<string>();

            try
            {
                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //Collect all the hashIDs of items in distroList and value of said items 
                    while (dataReader.Read())
                    {
                        list[0].Add(dataReader["valueOfItem"] + "");
                        list[1].Add(dataReader["hashID"] + "");
                    }

                    //close Data Reader
                    dataReader.Close();

                    //close Connection
                    this.CloseConnection();

                    //top to bottom 
                    for(int i=(list[0].Count -1); i >= 0; i--) {
                        Console.WriteLine(list[0][i]);
                        if(Convert.ToDouble(list[0][i]) <= cutValue) {
                            Console.WriteLine("cut value " + cutValue);
                            //subtract for the value left on the cutValue
                            cutValue = cutValue - Convert.ToDouble(list[0][i]);
                            //add itemt to cutList 
                            cutList.Add(list[1][i]);
                        }
                    }
                }
            }
            catch
            {
                
            }
            return cutList;
        }

        public bool setNewOwnerCut(List<string> cutList, string newOwnerID)
        {
            try 
            {
                //Open connection
                if (this.OpenConnection() == true)
                {
                    for (int i = 0; i < cutList.Count; i++)
                    {
                        //search for hashID and update newOwnerID
                        string query = "UPDATE distributionList SET newOwnerID='" + newOwnerID + "' WHERE hashID='"+ cutList[i] + "'";

                        //create mysql command
                        MySqlCommand cmd = new MySqlCommand();
                        //Assign the query using CommandText
                        cmd.CommandText = query;
                        //Assign the connection using Connection
                        cmd.Connection = connection;

                        //Execute query
                        cmd.ExecuteNonQuery();
                    }

                    //close connection
                    this.CloseConnection();
                    return true;
                }
            }
            catch
            {

            }
            return false;
        }

        public List<string> buildWinnerList(List<string> cutList, string lotteryID)
        {
            List<string> winnerList = new List<string>();
            try
            {
                string query = "SELECT * FROM distributionList WHERE lotteryID='"+ lotteryID +"'";

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //Read the data and store them in the list
                    while (dataReader.Read())
                    {
                        bool found = false;
                        string currentHashID = (dataReader["hashID"] + "");
                        //search through cutlist to see if the item is already reserved
                        for (int i = 0; i < cutList.Count; i++)
                        {
                            if (currentHashID.Equals(cutList[i]))
                            {
                                found = true;
                                //in the case that the hashID is already reserved for the cut list, we can't take said item
                            }
                        }
                        if (found == false)
                        {
                            winnerList.Add(currentHashID);
                        }
                    }

                    //close Data Reader
                    dataReader.Close();

                    //close Connection
                    this.CloseConnection();
                }
            }
            catch
            {

            }
            return winnerList;
        }

        public List<string> [] getdescriptionIDList(string lotteryID)
        {
            List<string> [] list = new List<string>[3];
            //description ID
            list[0] = new List<string>();
            //itemID
            list[1] = new List<string>();
            //hashID
            list[2] = new List<string>();
            try
            {
                //Open connection
                if (this.OpenConnection() == true)
                {
                    string query = "SELECT * FROM distributionList WHERE lotteryID='" + lotteryID + "'";
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //grab all the required data
                    while (dataReader.Read())
                    {
                        list[0].Add(dataReader["descriptionID"] + "");
                        list[1].Add(dataReader["itemID"] + "");
                        list[2].Add(dataReader["hashID"] + "");
                    }

                    //close Data Reader
                    dataReader.Close();

                    //close Connection
                    this.CloseConnection();
                }
            }
            catch
            {

            }

            return list;
        }

        public bool assignItemIDToList(List<string>[] descList)
        {
            try
            {
                //Open connection
                if (this.OpenConnection() == true)
                {
                    //assign itemID to all items we need to trade out
                    Console.WriteLine("counts " + descList[0].Count + descList[1].Count + descList[2].Count);
                    for (int i = 0; i < descList[0].Count; i++)
                    {
                        string query = "UPDATE distributionList SET itemID='" + descList[1][i] + "' WHERE hashID='" + descList[2][i] + "'";
                        //create mysql command
                        MySqlCommand cmd = new MySqlCommand(query, connection);
                        cmd.ExecuteNonQuery();

                    }
                    //close connection
                    this.CloseConnection();
                    return true;
                }
            }
            catch
            {

            }
            return false;
        }

        public string getItemID(string hashID)
        {
            string itemID = "";
            try
            {
                //Open connection
                if (this.OpenConnection() == true)
                {
                    string query = "SELECT * FROM distributionList WHERE hashID='" + hashID + "'";
                    //Create Command
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    //Create a data reader and Execute the command
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    //grab all the required data
                    while (dataReader.Read())
                    {
                        itemID = (dataReader["itemID"] + "");
                    }

                    //close Data Reader
                    dataReader.Close();

                    //close Connection
                    this.CloseConnection();
                }
            }
            catch
            {

            }
            return itemID;
        }

        public bool sendMail(string msg)
        {
            try
            {

                SmtpClient mySmtpClient = new SmtpClient("smtp.gmail.com");

                // set smtp-client with basicAuthentication
                mySmtpClient.UseDefaultCredentials = false;
                System.Net.NetworkCredential basicAuthenticationInfo = new
                   System.Net.NetworkCredential("devilbladz", "123456789_");
                mySmtpClient.Credentials = basicAuthenticationInfo;

                // add from,to mailaddresses
                MailAddress from = new MailAddress("devilbladz@gmail.com", "Stars Error Report");
                MailAddress to = new MailAddress("031247ryan@gmail.com", "Ryan");
                MailMessage myMail = new System.Net.Mail.MailMessage(from, to);

                // add ReplyTo
                MailAddress replyto = new MailAddress("devilbladz@gmail.com");
                myMail.ReplyTo = replyto;

                // set subject and encoding
                myMail.Subject = "Error Report GO STARS";
                myMail.SubjectEncoding = System.Text.Encoding.UTF8;

                // set body-message and encoding
                myMail.Body = msg;
                myMail.BodyEncoding = System.Text.Encoding.UTF8;
                // text or html
                myMail.IsBodyHtml = true;

                mySmtpClient.Send(myMail);
                return true;
            }

            catch (SmtpException ex)
            {
                throw new ApplicationException
                  ("SmtpException has occured: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

        public bool updateLiveLottoWinner(string winnerID)
        {
            try
            {
                string query = "UPDATE liveLottery SET winnerID='"+ winnerID +"', distroComplete='True'";

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //create mysql command
                    MySqlCommand cmd = new MySqlCommand();
                    //Assign the query using CommandText
                    cmd.CommandText = query;
                    //Assign the connection using Connection
                    cmd.Connection = connection;

                    //Execute query
                    cmd.ExecuteNonQuery();

                    //close connection
                    this.CloseConnection();
                    return true;
                }
            }
            catch
            {

            }
            return false;
        } 
        
    }
}
