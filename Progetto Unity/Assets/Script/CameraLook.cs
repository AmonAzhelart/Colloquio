using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.Colloquio.SimpleHostile
{

    public class CameraLook : MonoBehaviourPunCallbacks
    {

        #region Variabili


        public static bool cursorLocked = true;

        public Transform player;
        public Transform cams;
        public Transform weapon;
        

        [SerializeField] 
        float xSesitivity;
        [SerializeField] 
        float ySesitivity;
        [SerializeField] 
        float maxAngle;

        private Quaternion camCenter;

        #endregion

        #region Monobehaviour Callbacks

        void Start()
        {
            camCenter = cams.localRotation; // imposta la rotazione originale per la camera
        }

        // Update is called once per frame
        void Update()
        {
            if(!photonView.IsMine) return;
            if(Pause.paused) return;
            SetY();
            SetX();
            UpdateCursorLock();
        }

        #endregion

        #region Private Methods

        void SetY()
        {
            // Permette di modificare la rotazione della telecamera dell'asse Y     
            float t_input = Input.GetAxis("Mouse Y")* ySesitivity * Time.deltaTime;
            Quaternion t_adj = Quaternion.AngleAxis(t_input, -Vector3.right);
            Quaternion t_delta = cams.localRotation * t_adj;

            // Permette di avere un angolo massimo di spostamento della telecamera
            if(Quaternion.Angle(camCenter, t_delta)< maxAngle)
            {
                cams.localRotation = t_delta;
                
                
            }
            
            
        }



        void SetX()
        {
            // Permette di modificare la rotazione della telecamera dell'asse X   
            float t_input = Input.GetAxis("Mouse X")* xSesitivity * Time.deltaTime;
            Quaternion t_adj = Quaternion.AngleAxis(t_input, Vector3.up);
            Quaternion t_delta = player.localRotation * t_adj;
            player.localRotation = t_delta;
            
        }


        //Funzione che permette cliccando esc di far apparire o scomparire il puntatore del mouse
        void UpdateCursorLock()
        {
            if(cursorLocked)
            {
                Cursor.lockState= CursorLockMode.Locked;
                Cursor.visible= false;

                if(Input.GetKeyDown(KeyCode.Escape))
                {
                    GameObject.Find("Pause").GetComponent<Pause>().TogglePause();
                    
                }
            }
            else
            {
            
                Cursor.lockState= CursorLockMode.None;
                Cursor.visible= true;

                if(Input.GetKeyDown(KeyCode.Escape))
                {
                    GameObject.Find("Pause").GetComponent<Pause>().TogglePause();
                    
                }
                
            }

            
        }

        #endregion
    
    }
}
