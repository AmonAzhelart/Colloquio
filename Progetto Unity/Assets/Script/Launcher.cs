using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

namespace Com.Colloquio.SimpleHostile
{

    [System.Serializable]
    public class ProfileData
    {
        public string username;
        public int level;
        public int xp;

        public ProfileData()
        {
            this.username= "DEFAULT USERNAME";
            this.level = 0;
            this.xp = 0;
        }

        public ProfileData(string u, int l, int x)
        {
            this.username= u;
            this.level = l;
            this.xp = x;
        }

        
    }

    public class Launcher : MonoBehaviourPunCallbacks
    {

        public InputField usernameField;
        public InputField roomnameField;
        public Slider maxPlayersSlider;
        public Text maxPlayersValue;
        public static ProfileData myProfile = new ProfileData();

        public GameObject tabMain;
        public GameObject tabRooms;
        public GameObject tabCreate;

        public GameObject buttonRoom;

        private List <RoomInfo> roomList;
        


        public void Awake() 
        {
            PhotonNetwork.AutomaticallySyncScene = true;
        
            myProfile = Data.LoadProfile();
            usernameField.text = myProfile.username;

            Connect();    
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("Connesso");
            PhotonNetwork.JoinLobby();
            base.OnConnectedToMaster();
        }

        public override void OnJoinedRoom()
        {
            StartGame();
            base.OnJoinedRoom();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Create();

            base.OnJoinRandomFailed(returnCode, message);    
        }

        public void Connect ()
        {
            Debug.Log("Provando a connettermi...");
            PhotonNetwork.GameVersion="0.0.0";
            PhotonNetwork.ConnectUsingSettings();
        }
       
        public void Join()
        {
            VerifyUsername();
            StartGame();
            PhotonNetwork.JoinRandomRoom();
        }

        public void Create ()
        {
            //Funzione che permette di creare una lobby con determinate impostazioni
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = (byte) maxPlayersSlider.value;

            ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable();
            properties.Add("map",0);
            options.CustomRoomProperties = properties;

            PhotonNetwork.CreateRoom(roomnameField.text, options);
        }

        public void ChangeMaxPlayerSlider (float t_value)
        {
            maxPlayersValue.text = Mathf.RoundToInt(t_value).ToString();
        }

        public void TabCloseAll()
        {
            tabMain.SetActive(false);
            tabRooms.SetActive(false);
            tabCreate.SetActive(false);
        }

        public void TabOpenMain()
        {
            TabCloseAll();
            tabMain.SetActive(true);
        }

        public void TabOpenRooms()
        {
            TabCloseAll();
            tabRooms.SetActive(true);
        }

        public void TabOpenCreate ()
        {
            TabCloseAll();
            tabCreate.SetActive(true);
        }

        private void ClearRoomList()
        {
            Transform content = tabRooms.transform.Find("Scroll View/Viewport/Content");
            foreach(Transform a in content) Destroy(a.gameObject);
        }

        //Questa funzione permette di vedere tutte le stanze esistenti ed aggiungere un event AddListener ai bottoni, in modo da distinguere le stanze e decidere dove entrare
        public override void OnRoomListUpdate(List<RoomInfo> p_list)
        {

            roomList = p_list;
            ClearRoomList();

            Debug.Log("Loaded rooms @"+Time.time);
            Transform content = tabRooms.transform.Find("Scroll View/Viewport/Content");

            foreach(RoomInfo a in roomList)
            {

                GameObject newRoomButton = Instantiate(buttonRoom, content) as GameObject;

                newRoomButton.transform.Find("Name").GetComponent<Text>().text = a.Name;
                newRoomButton.transform.Find("Players").GetComponent<Text>().text = a.PlayerCount + " / " + a.MaxPlayers;

                newRoomButton.GetComponent<Button>().onClick.AddListener(delegate{JoinRoom(newRoomButton.transform,a.PlayerCount,a.MaxPlayers);});
            }

            base.OnRoomListUpdate(roomList);
        }

        //Funzione che viene richiamata quando si clicca sul bottone e permette di selezionare la stanza corretta, ma permette anche di controllare se è piena o no
        public void JoinRoom(Transform p_button,int attuali,int max)
        {
            Debug.Log("Joining room @ "+ Time.time);
            string t_roomName = p_button.transform.Find("Name").GetComponent<Text>().text;
            if(attuali < max)
            {
                PhotonNetwork.JoinRoom(t_roomName);
            }
            
            
        }

        private void VerifyUsername()
        {
            if(string.IsNullOrEmpty(usernameField.text))
            {
                myProfile.username = "Random_User"+ Random.Range(100, 1000);
            }
            else
            {
                myProfile.username= usernameField.text;
            }
        }

        public void StartGame()
        {

            Debug.Log("Start game");
            
            VerifyUsername();

            if(PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                Data.SaveProfile(myProfile);
                PhotonNetwork.LoadLevel(1);
            }
        }

    }
}

