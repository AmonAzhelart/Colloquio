using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

namespace Com.Colloquio.SimpleHostile
{

    //Permette di gestire il menù di pausa, con le due funzioni TogglePause, che sarebbe il tasto riprendi e Quit che serve per ritornare al menù
    public class Pause : MonoBehaviour
    {

        public static bool paused = false;
        public bool disconnecting = false;

        public void TogglePause()
        {
            Debug.Log("hello");
            if(disconnecting) return;

            paused = !paused;

            transform.GetChild(0).gameObject.SetActive(paused);
            Cursor.lockState = (paused) ? CursorLockMode.None : CursorLockMode.Confined;
            Cursor.visible = paused;
        }

        public void Quit()
        {
            disconnecting = true;
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene(0);
        }

}
}


