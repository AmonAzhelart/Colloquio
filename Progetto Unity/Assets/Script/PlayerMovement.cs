using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;


namespace Com.Colloquio.SimpleHostile
{
    public class PlayerMovement : MonoBehaviourPunCallbacks, IPunObservable 
    {

        #region Variabili

        public float speed;
        public float sprintModifier; // variabile usata per gestire il modificatore di corsa
        public float jumpforce; // forza di salto
        private Rigidbody rig;
        public Camera normalCam;
        public GameObject cameraParent;
        public Transform weaponParent;
        public Transform CameraPosition;
        public Transform goundDetector;
        public LayerMask ground;
        public bool isGrounded; //variabile che controlla se sei a terra o no
        public int max_health; // vita massima
        private Vector3 weaponParentOrigin;
        private Vector3 targetWeaponBobPosition;

        [HideInInspector] public ProfileData playerProfile; // permette di creare una variabile di tipo Profile data in modo da salvare le impostazioni
        public TextMeshPro playerUsername;

        public bool isDie;
        

        private float movementCounter;
        private float idleCounter;
        private Transform hud_healthbar;
        private Text hud_username;
        private Text hud_mykills;
        private Text hud_mydeaths;
        private float baseFOV;
        private float sprintFOVModifier = 1.25f;

        private int current_health; 
        private Text hud_ammo;

        private Manager manager;
        private Weapon weapon;

        private float aimAngle;

        #endregion

        #region Photon Callbacks

        //funzione che permette di sincronizzare i movimenti in multiplayer
        public void OnPhotonSerializeView(PhotonStream p_stream, PhotonMessageInfo p_message)
        {
            if(p_stream.IsWriting)
            {
                p_stream.SendNext((int)(CameraPosition.localEulerAngles.x *100f));
            }
            else
            {
                aimAngle = (int)p_stream.ReceiveNext()/100f;
            }
        }

         private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }
        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        #endregion

        #region Monobehaviour Callbacks

        void Start()
        {

            isDie = false; 
            current_health= max_health;
            manager = GameObject.Find("Manager").GetComponent<Manager>();
            weapon = GetComponent<Weapon>();
            
            
            cameraParent.SetActive(photonView.IsMine);
            
            if(!photonView.IsMine) gameObject.layer= 9; // assegno un layer al player
            
            baseFOV=normalCam.fieldOfView;
            rig = GetComponent<Rigidbody>(); // Permette di prendere il rigidbody di dove è contenuto lo script in questo caso del player
            weaponParentOrigin = weaponParent.localPosition;

            if(photonView.IsMine)
            {
                //serve a memorizzare l'hud e poterla modificare 
                hud_healthbar=GameObject.Find("HUD/Health/Bar").transform; 
                hud_ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
                hud_username = GameObject.Find("HUD/Username/Text").GetComponent<Text>();
                hud_mykills = GameObject.Find("Canvas/HUD/Stats/Kills/Text").GetComponent<Text>();
                hud_mydeaths = GameObject.Find("Canvas/HUD/Stats/Deaths/Text").GetComponent<Text>();

                RefreshHealthBar(); // funzione che permette il refresh della barra della vita
                hud_username.text = Launcher.myProfile.username;
                photonView.RPC("SyncProfile" ,RpcTarget.All ,Launcher.myProfile.username,Launcher.myProfile.level,Launcher.myProfile.xp); // funzione che permette di sincronizzare le persone all'interno della lobby
  
                
            }
            
        } 

        
        [PunRPC]
        private void SyncProfile(string p_username, int p_level, int p_xp)
        {
            playerProfile = new ProfileData(p_username, p_level, p_xp);
            playerUsername.text = playerProfile.username;

        }
        

        void FixedUpdate()
        {
            if(!photonView.IsMine) 
            {
                RefreshMultiplayerState();
                return;
            }

            //Input
            float Horizontal= Input.GetAxis("Horizontal"); // memoirzza nella variabile quando noi premiamo ad esemptio a o d per muoversi
            float Vertical= Input.GetAxis("Vertical"); // stessa cosa di sopra ma per w e s per muoversi avanti e indietro
            
            //Controls
            bool sprint = Input.GetKey(KeyCode.LeftShift); //Prende in input il valore di quando si clicca lo shift sinistro
            bool jump = Input.GetKey(KeyCode.Space);  //Prende in input il valore di quando si clicca lo spazio
            bool pause= Input.GetKeyDown(KeyCode.Escape); 


            //States
            isGrounded = Physics.Raycast(goundDetector.position, Vector3.down, 0.1f); // Permette di creare un raycast sotto il personaggio che permette di vedere quando collidi con il terreno
            bool isJumping = jump && isGrounded; // Controllo se sto saltando
            bool isSprinting = sprint && Vertical >0 && !isJumping && isGrounded; // Controllo se sto sprintando
            
            //Pause


            if(Pause.paused)
            {
                Horizontal=0f;
                Vertical=0f;
                sprint=false;
                jump=false;
                pause=false;
                isGrounded=false;
                isJumping=false;
                isSprinting=false;
            }

            //Jumping
            if(isJumping)
            {
                rig.AddForce(Vector3.up * jumpforce); // funzione che aggiunge una forza al rigidbody del player per farlo saltare
            }


            //Movement
            Vector3 direction = new Vector3(Horizontal, 0 , Vertical); // Permette di creare un vettore con le variabili che gli abbiamo passato in modo tale da poter calcolare dove di deve muovere il pg
            direction.Normalize(); // permette di normalizzare il vettore direzione

            float t_adjustedSpeed= speed;
            if(isSprinting)
            {
                t_adjustedSpeed *= sprintModifier; // aumento velocità
            }

            Vector3 t_targetVelocity = transform.TransformDirection(direction) * t_adjustedSpeed * Time.deltaTime; // applica al rigidbody una direzione ed una velicità moltiplicato per il deltaTime che permette di fare questi calcoli indipendentemente dal framerate
            t_targetVelocity.y = rig.velocity.y;
            rig.velocity = t_targetVelocity;
        
            //FOV
            if(isSprinting)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView,baseFOV * sprintFOVModifier, Time.deltaTime * 8f); // La funzione Mathf.Lerp permette di fare il cambio della fov in maniera graduale
            }
            else
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView,baseFOV, Time.deltaTime * 8f);
            }
        
            //Head Bob permette di dare un effetto respiro all'arma in modo da non essere ferma
            
            if(Horizontal == 0 && Vertical == 0) 
            {
                HeadBob(idleCounter, 0.025f,0.025f);
                idleCounter+= Time.deltaTime;
                weaponParent.localPosition= Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime *2f);
            }
            else if(!isSprinting)
            {
                HeadBob(movementCounter,0.06f,0.06f);
                movementCounter+= Time.deltaTime *3f;
                weaponParent.localPosition= Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime *6f);
            }
            else 
            {
                HeadBob(movementCounter,0.08f,0.08f);
                movementCounter+= Time.deltaTime *5f;
                weaponParent.localPosition= Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime *10f);
            }

            // HUD Refresh

            RefreshHealthBar();
            weapon.RefreshAmmo(hud_ammo);
            

        }

        #endregion

        #region Private Methods

        void RefreshMultiplayerState() // permette di sincronizzare i movimenti delle persone in lobby specialmente dell'arma
        {
            float cacheEulY = CameraPosition.localEulerAngles.y;
            Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle,Vector3.right);
            CameraPosition.rotation = Quaternion.Slerp(CameraPosition.rotation,targetRotation, Time.deltaTime *8f);
            Vector3 finalRotation= CameraPosition.localEulerAngles;
            finalRotation.y = cacheEulY;
            CameraPosition.localEulerAngles=finalRotation;
        }

        void HeadBob(float p_z, float p_x_intensity, float p_y_intensity) // funzione che applica il movimento respiro dell'arma
        {
            float t_aim_adj = 1f;
            if(weapon.IsAiming) t_aim_adj = 0.1f;
            targetWeaponBobPosition = weaponParentOrigin + new Vector3(Mathf.Cos(p_z)*p_x_intensity *t_aim_adj,Mathf.Sin(p_z *2) *p_y_intensity * t_aim_adj,0);
        }

        void RefreshHealthBar() // funzione dedicata al refresh della barra della vita
        {
            float t_health_ratio = (float)current_health / (float)max_health;
            hud_healthbar.localScale = Vector3.Lerp(hud_healthbar.localScale ,new Vector3(t_health_ratio,1 , 1), Time.deltaTime * 8f);
        }


        #endregion

        #region Public Methods

        [PunRPC]
        public void TakeDamage(int p_damage, int p_actor)
        {
            if(photonView.IsMine)
            {
                

                if(current_health<=0 && !isDie)
                {
                    isDie = true;
                    manager.Spawn();
                    manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1); 
                    if(p_actor >=0) manager.ChangeStat_S(p_actor,0,1);
                    PhotonNetwork.Destroy(gameObject);
                   
                }

                current_health -= p_damage;
                RefreshHealthBar();
                
                
            }
            
        }

        #endregion

    }
}
