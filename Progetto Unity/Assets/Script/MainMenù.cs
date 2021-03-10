using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Com.Colloquio.SimpleHostile
{

    //Tutte queste funzioni servono solo per assegnare ai bottoni del menù le relative istruzioni sul da farsi una volta cliccati
    public class MainMenù : MonoBehaviour
    {

        public Launcher launcher;

        private void Start() 
        {
            
            Pause.paused=false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
        }

        public void JoinMatch()
        {
            launcher.Join();
        }

        public void CreateMatch()
        {
            launcher.Create();
        }

        public void QuitGame()
        {
            Application.Quit();
        }

    }
}


