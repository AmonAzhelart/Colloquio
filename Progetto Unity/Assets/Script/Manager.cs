using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;


namespace Com.Colloquio.SimpleHostile
{

    public class PlayerInfo
    {
        public ProfileData profile;
        public int actor;
        public short kills;
        public short deaths;
    

        public PlayerInfo (ProfileData p, int a, short k, short d)
        {
            this.profile = p;
            this.actor = a;
            this.kills = k;
            this.deaths = d;
        }
    }

    public enum GameState
    {
        Waiting =0,
        Starting =1,
        Playing = 2,
        Ending = 3
    }


    public class Manager : MonoBehaviourPunCallbacks, IOnEventCallback
    {

        #region Variable
        public int mainmenu =0;
        public int killcount = 3;
        public GameObject mapcam;
        public int matchLenght=120 ; // Durata massima di un match

        // Start is called before the first frame update
        public string player_prefab_string;
        public GameObject player_prefab;
        public Transform[] spawn_point;

        public List<PlayerInfo> playerInfo = new List<PlayerInfo>(); //Lista dei player presenti nella lobby
        public int myind;

        private Text hud_mykills;
        private Text hud_mydeaths;
        private Text hud_timer;
        private Transform hud_leaderboard;
        private Transform hud_endgame;
        public GameObject HUD;

        private int currentMatchTime;
        private Coroutine timerCoroutine;

        private GameState state = GameState.Waiting;

        #endregion

        #region enum

        public enum EventCodes: byte
        {
            NewPlayer,
            UpdatePlayers,
            ChangeStat,
            RefreshTimer
        }

        #endregion
        
        #region Photon

        //Permette di aggiungere degli eventi ad esempio quando si connette un nuovo giocatore o quando si devono cambiare le sue relative stats
        public void OnEvent(EventData photonEvent)
        {
            if(photonEvent.Code >= 200) return; // >= 200 perchè è di default così
            EventCodes e = (EventCodes) photonEvent.Code;
            object[] o = (object[]) photonEvent.CustomData;

            switch(e)
            {
                case EventCodes.NewPlayer:
                    NewPlayer_R(o);
                    break;
                
                case EventCodes.UpdatePlayers:
                    UpdatePlayers_R(o);
                    break;
                
                case EventCodes.ChangeStat:
                    ChangeStat_R(o);
                    break;

                case EventCodes.RefreshTimer:
                    RefreshTimer_R(o);
                    break;
            }
        }

        // funzione che permette di uscire dalla lobby

        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            SceneManager.LoadScene(mainmenu);
        }

        #endregion
        
        #region Monobehaviour Callbacks
        //Permette di inizializzare tutte le informazioni quando si avvia la partita
        private void Start()
        {
            mapcam.SetActive(false);
            ValidateConnection();
            InitializeUI();
            InizializeTimer();
            NewPlayer_S(Launcher.myProfile);
            Spawn();
        }

         private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }
        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }   

        private void Update()
        {
            if(state == GameState.Ending) // controllo se la partita è finita
            {
                return;
            }
            if(Input.GetKeyDown(KeyCode.Tab)) // permette di attivare o disattivare la leaderboard
            {
                if(hud_leaderboard.gameObject.activeSelf) hud_leaderboard.gameObject.SetActive(false);
                else Leaderboard(hud_leaderboard);
            }
        }

        #endregion
       
        #region Inizilize

        private void InizializeTimer() // Permette di inizializzare il timer della partita
        {
           currentMatchTime = 120;
           RefreshTimerUI();

           if(PhotonNetwork.IsMasterClient)
           {
               timerCoroutine = StartCoroutine(Timer()); 
           }
        }

        //Funzione che permette di inizializzare tutta l'HUD
        private void InitializeUI()
        {
            hud_mykills = GameObject.Find("Canvas/HUD/Stats/Kills/Text").GetComponent<Text>();
            hud_mydeaths = GameObject.Find("Canvas/HUD/Stats/Deaths/Text").GetComponent<Text>();
            hud_timer= GameObject.Find("HUD/Timer").GetComponent<Text>();
            hud_leaderboard = GameObject.Find("HUD").transform.Find("Leaderboard").transform;
            hud_endgame = GameObject.Find("Canvas").transform.Find("End Game").transform;
            RefreshMyStats();
        }
        
        #endregion
       
        #region Refresh

        //funzione usata per Refreshare l'hud del timer
        private void RefreshTimerUI()
        {
           string minutes = (currentMatchTime /60).ToString("00");
           string seconds = (currentMatchTime  % 60).ToString("00");
           hud_timer.text = $"{minutes}:{seconds}";
        }

        //Funzione usata per fare un refresh delle stats del player
        private void RefreshMyStats()
        {
            
            if(playerInfo.Count > myind)
            {
                hud_mykills.text = $"{playerInfo[myind].kills} kills";
                hud_mydeaths.text = $"{playerInfo[myind].deaths} deaths";
            }
            else
            {
                hud_mykills.text = "0 kills";
                hud_mydeaths.text = "0 deaths";
            }

            if(hud_leaderboard.gameObject.activeSelf) Leaderboard(hud_leaderboard);
        }


        #endregion

        #region Leaderboard

        //Funzione che si occupa della gestione della Leaderboard

        private void Leaderboard(Transform p_lb)
        {
            //pulizia
            for(int i=2; i< p_lb.childCount;i++)
            {
                Destroy(p_lb.GetChild(i).gameObject);
            }

            //cache prefab

            GameObject playercard = p_lb.GetChild(1).gameObject;
            playercard.SetActive(false);

            //sort
            List<PlayerInfo> sorted = SortPlayers(playerInfo);

            //display
            bool t_alternateColors = false;

            //Foreach che scorre nella lista delle persone presenti nella lobby e prende le informazioni per poterle stampare nella leaderboard
            foreach(PlayerInfo a in sorted)
            {
                Debug.Log(a.profile.username);
                GameObject newcard = Instantiate(playercard, p_lb)as GameObject;

                if(t_alternateColors) newcard.GetComponent<Image>().color = new Color32(0,0,0,180);
                t_alternateColors = !t_alternateColors;

                newcard.transform.Find("Rank Value").GetComponent<Text>().text = a.profile.level.ToString ("00");
                newcard.transform.Find("Username").GetComponent<Text>().text = a.profile.username;
                newcard.transform.Find("Score Value").GetComponent<Text>().text = (a.kills *100).ToString();
                newcard.transform.Find("Kills Value").GetComponent<Text>().text = a.kills.ToString();
                newcard.transform.Find("Deaths Value").GetComponent<Text>().text = a.deaths.ToString();

                newcard.SetActive(true);
            }

            p_lb.gameObject.SetActive(true);
        }


        //Funzione che permette di ordinare i player nella leaderboard
        private List<PlayerInfo> SortPlayers(List <PlayerInfo> p_info)
        {
            List<PlayerInfo> sorted = new List <PlayerInfo>();

            while( sorted.Count <p_info.Count)
            {
                //set defaults
                short highest = -1;
                PlayerInfo selection = p_info[0];

                //prossimo player più alto
                foreach(PlayerInfo a in p_info)
                {
                    if(sorted.Contains(a)) continue;
                    if(a.kills > highest)
                    {
                        selection = a;
                        highest = a.kills;
                    }
                }

                //add player
                sorted.Add(selection);
            }

            return sorted;
        }


        #endregion     

        #region Gestione Partita

        //Funzione che permette di spawnare un player all'inizio o respawnarlo una volta morto
        public void Spawn()
        {
            Transform t_spawn = spawn_point[Random.Range(0, spawn_point.Length)];
            if(PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Instantiate(player_prefab_string, t_spawn.position, t_spawn.rotation);
            }
            else
            {
                GameObject newPlayer = Instantiate(player_prefab, t_spawn.position, t_spawn.rotation) as GameObject;
            }
            
        }

        private void ValidateConnection()
        {
            if(PhotonNetwork.IsConnected) return;
            SceneManager.LoadScene(mainmenu);
        }

        private void StateCheck()
        {
            if(state == GameState.Ending)
            {
                EndGame();
            }
        }
        
        private void ScoreCheck()
        {
            bool detectwin = false;

            foreach(PlayerInfo a in playerInfo)
            {
                if(a.kills >= killcount)
                {
                    detectwin = true;
                    break;
                }
            }

            if(detectwin)
            {
                if(PhotonNetwork.IsMasterClient && state != GameState.Ending)
                {
                    UpdatePlayers_S((int) GameState.Ending,playerInfo);
                }
            }
        }

        //Funzione che permette di azionare la Leaderboard a fine partita
        private void GameOver()
        {
            Leaderboard(hud_leaderboard);   
            StartCoroutine("End",10);        
        }

        //Funzione che si occupa della gestione della fine partita 
        private void EndGame()
        {
            //set game state to ending
            state= GameState.Ending;

            //set timer to 0

            if(timerCoroutine != null) StopCoroutine(timerCoroutine);
            currentMatchTime =0;
            RefreshTimerUI();


            if(PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.DestroyAll();
                PhotonNetwork.CurrentRoom.IsVisible = false;
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }

            mapcam.SetActive(true);

            hud_endgame.gameObject.SetActive(true);
            Leaderboard(hud_endgame.Find("Leaderboard"));

            StartCoroutine(End(6f));
        }

        #endregion
       
        #region Send & Recive 

        // Permette di sistemare tutti gli array e prepararli per la gestione dei player in multiplayer, quindi gestire un nuovo player o modificare i dati dei player già in gioco ad esempio quando fanno una kill

        public void NewPlayer_S(ProfileData p)
        {
            object[] package = new object[6];
            package[0]= p.username;
            package[1]= p.level;
            package[2]= p.xp;
            package[3]= PhotonNetwork.LocalPlayer.ActorNumber;
            package[4]= (short) 0;
            package[5]= (short) 0;

            PhotonNetwork.RaiseEvent(
                    (byte)EventCodes.NewPlayer,
                    package,
                    new RaiseEventOptions { Receivers = ReceiverGroup.All },
                    new SendOptions { Reliability = true }
                );

        }

        public void NewPlayer_R (object[] data)
        {
            PlayerInfo p = new PlayerInfo
            (
                new ProfileData
                    (
                        (string) data[0],
                        (int) data[1],
                        (int) data[2]
                    ),
                    (int) data[3],
                    (short) data[4],
                    (short) data[5]
            );

            playerInfo.Add(p);

            UpdatePlayers_S((int)state, playerInfo);
        }

        public void UpdatePlayers_S (int state, List<PlayerInfo> info)
        {
            object[] package = new object[info.Count + 1];

            for(int i =0; i<info.Count; i++)
            {
                object[] piece = new object[6];
                
                piece[0] = info[i].profile.username;
                piece[1] = info[i].profile.level;
                piece[2] = info[i].profile.xp;
                piece[3] = info[i].actor;
                piece[4] = info[i].kills;
                piece[5] = info[i].deaths;

                package[i + 1]=piece;

            }

            PhotonNetwork.RaiseEvent
                (
                    (byte)EventCodes.UpdatePlayers,
                    package,
                    new RaiseEventOptions { Receivers = ReceiverGroup.All },
                    new SendOptions { Reliability = true }
                );
        }

        public void UpdatePlayers_R(object[] data)
        {
            state = (GameState) data[0];
            playerInfo = new List<PlayerInfo>();
            for(int i =1; i<data.Length; i++)
            {
                object[] extract = (object[]) data[i];

                PlayerInfo p = new PlayerInfo
                (
                    new ProfileData
                    (
                        (string) extract[0],
                        (int) extract[1],
                        (int) extract[2]
                    ),
                    (int) extract[3],
                    (short) extract[4],
                    (short) extract[5]
                );

                playerInfo.Add(p);
                
                if(PhotonNetwork.LocalPlayer.ActorNumber == p.actor) myind =i - 1;
            }
            StateCheck();
        }

        public void ChangeStat_S (int actor, byte stat, byte amt)
        {

            object[] package = new object[] { actor, stat, amt };

            PhotonNetwork.RaiseEvent(
                (byte)EventCodes.ChangeStat,
                package,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                new SendOptions { Reliability = true }
            );
        }

        public void ChangeStat_R (object[] data)
        {
            int actor = (int) data[0];
            byte stat = (byte) data[1];
            byte amt = (byte) data[2];

            for(int i=0; i< playerInfo.Count; i++)
            {
                if(playerInfo[i].actor == actor)
                {
                    switch(stat)
                    {
                        case 0: //Kills
                            playerInfo[i].kills += amt;
                            Debug.Log($"Player {playerInfo[i].profile.username} : kills = {playerInfo[i].kills}");
                            break;

                        case 1: //death
                            playerInfo[i].deaths += amt;
                            Debug.Log($"Player {playerInfo[i].profile.username} : deaths = {playerInfo[i].deaths}");
                            break;
                    }

                    if(i==myind) RefreshMyStats();
                    if(hud_leaderboard.gameObject.activeSelf) Leaderboard(hud_leaderboard);

                    break;
                }
            }

            ScoreCheck();
        }

        public void RefreshTimer_S()
        {
            object[] package = new object[] {currentMatchTime};

            PhotonNetwork.RaiseEvent(
                (byte)EventCodes.RefreshTimer,
                package,
                new RaiseEventOptions {Receivers = ReceiverGroup.All},
                new SendOptions {Reliability = true}
            );
        }

        public void RefreshTimer_R(object[] data)
        {
            currentMatchTime = (int)data[0];
            RefreshTimerUI();
            if(currentMatchTime<=1)
            {
                GameOver();
            }
        }
        #endregion

        #region IEnumerator
        //Permette di gestire il timer e controllare se è finito oppure no
        private IEnumerator Timer()
        {
            yield return new WaitForSeconds(1f);
            currentMatchTime-=1;

            if(currentMatchTime <=0)
            {
                timerCoroutine = null;
                GameOver();
            }
            else
            {
                RefreshTimer_S();
                timerCoroutine = StartCoroutine(Timer());
            }
        }

        //Timer di fine partita
        private IEnumerator End(float p_wait)
        {
            yield return new WaitForSeconds(p_wait);

            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
        
        #endregion

    }

}
