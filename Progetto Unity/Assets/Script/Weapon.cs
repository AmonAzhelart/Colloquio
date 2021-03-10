using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;

namespace Com.Colloquio.SimpleHostile
{


    public class Weapon : MonoBehaviourPunCallbacks
    {
        #region Variabili
        public Gun[] loadout;
        public Transform weaponParent;
        public GameObject bulletholePrefab;
        public LayerMask canBeShot;
        public bool IsAiming = false;

        private float currentCooldown;
        private int currentIndex;
        private GameObject currentWeapon;

        private bool isReloading;

        #endregion

        #region Monobehaviour Callbacks
        void Start()
        {
            foreach(Gun u in loadout) u.Initialize(); // inizializza tutte le armi e le equipaggia
            Equip(0);
        }

        // Update is called once per frame
        void Update()
        {
            if(Pause.paused && photonView.IsMine) return;
            // controllo se equipaggiare pistola (Alpha 1) o mitra (Alpha 2)
            if(photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1)) photonView.RPC("Equip",RpcTarget.All,0); 
            if(photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha2)) photonView.RPC("Equip",RpcTarget.All,1);
            
            if(currentWeapon !=null)
            {
                if(photonView.IsMine)
                {
                    Aim(Input.GetMouseButton(1)); // permette di mirare quando clicchi il sinistro del mouse

                    if(loadout[currentIndex].burst != 1) // se una mitra permette il fuoco automatico
                    {
                        if(Input.GetMouseButtonDown(0) && currentCooldown<=0)// col click destro si spara
                        {   
                            if(loadout[currentIndex].FireBullet()) photonView.RPC("Shoot",RpcTarget.All);
                            else StartCoroutine(Reload(loadout[currentIndex].reloadTime));
                        }
                    }

                    else
                    {
                        
                        if(Input.GetMouseButton(0) && currentCooldown<=0)// col click destro si spara
                        {   
                            if(loadout[currentIndex].FireBullet()) photonView.RPC("Shoot",RpcTarget.All);
                            else StartCoroutine(Reload(loadout[currentIndex].reloadTime));
                        }

                    }

                    

                    if(Input.GetKeyDown(KeyCode.R)) StartCoroutine(Reload(loadout[currentIndex].reloadTime));
                    
                    //cooldown
                    if(currentCooldown >0) currentCooldown -= Time.deltaTime;
                }
                

                //weapon position elasticity
                currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime *4f); 
            }
            
        }

        #endregion

        #region Private Methods 

        //Permette di avere un coldawn della ricarica che varia in base all'arma usata

        IEnumerator Reload(float p_wait)
        {
            isReloading = true;
            currentWeapon.SetActive(false);

            yield return new WaitForSeconds(p_wait);

            loadout[currentIndex].Reload();
            currentWeapon.SetActive(true);
            isReloading= false;
        }

        //Funzione che permette di equipaggiare l'arma 
        [PunRPC]
        void Equip(int p_ind)
        {
            if(currentWeapon!= null)
            {
                if(isReloading)StopCoroutine("Reload");
                Destroy(currentWeapon);
            }
            
            currentIndex = p_ind;

            GameObject t_newEquipment = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
            t_newEquipment.transform.localPosition = Vector3.zero;
            t_newEquipment.transform.localEulerAngles = Vector3.zero;
            t_newEquipment.GetComponent<Sway>().enabled = photonView.IsMine;

            currentWeapon = t_newEquipment;
        }

        //funzione che permette di mirare cioò sposta l'ogetto weapon in posizione di aim
        void Aim(bool p_isAiming)
        {
            IsAiming = p_isAiming;
            Transform t_anchor = currentWeapon.transform.Find("Anchor");
            Transform t_state_ads = currentWeapon.transform.Find("States/ADS");
            Transform t_state_hip = currentWeapon.transform.Find("States/Hip");

            if(p_isAiming)
            {
                //aim
                t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
            }
            else
            {
                //hip
                t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
            }

        }


        //Funzione che permette lo sparo tramite un raycast che è una linea che viene tracciata in una determinata direzione e quando entra in collissione con qualcosa si genere il foro del proiettile
        [PunRPC]
        void Shoot()
        {
            Transform t_spawn = transform.Find("Camera/NormalCamera");

            //bloom
            Vector3 t_bloom=t_spawn.position + t_spawn.forward * 1000f;
            t_bloom=t_spawn.position + t_spawn.forward * 1000f;
            t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.up;
            t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.right;
            t_bloom -= t_spawn.position;
            t_bloom.Normalize();
                         
            //cooldown
            currentCooldown = loadout[currentIndex].firerate;
            
            //raycast
            RaycastHit t_hit = new RaycastHit();
            if(Physics.Raycast(t_spawn.position,t_bloom, out t_hit, 1000f, canBeShot))
            {
                    
                GameObject t_newHole = Instantiate(bulletholePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
                t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
                Destroy(t_newHole,5f);

                if(photonView.IsMine)
                {
                    //shooting other player on network
                    if(t_hit.collider.gameObject.layer==9)
                    {
                        //RPC Call to Damage Player Goes Here
                        t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage",RpcTarget.All,loadout[currentIndex].damage, PhotonNetwork.LocalPlayer.ActorNumber);
                    }
                }

            }

            //gun fx
            currentWeapon.transform.Rotate(-loadout[currentIndex].recoil,0,0);
            currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickback;
        
        }

        //Funzione che permette di togliere vita a un player colpito

        [PunRPC]
        private void TakeDamage(int p_damage, int p_actor)
        {
            GetComponent<PlayerMovement>().TakeDamage(p_damage, p_actor);
        }


        #endregion
    
        #region Public Methods

        //Funzione che permette di refreshare l'hud delle munizioni totale ed equipaggiate

        public void RefreshAmmo(Text p_text)
        {
            int t_clip = loadout[currentIndex].GetClip();
            int t_stash = loadout[currentIndex].GetStash();

            p_text.text = t_clip.ToString() + " / " + t_stash.ToString();
        }
        
        #endregion

    }
    
}