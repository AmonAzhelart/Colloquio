using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Com.Colloquio.SimpleHostile
{
    public class Data : MonoBehaviour
    {

        //Funzione che permette di salvare le impostazioni di un account in un file che viene generato nella cartella di installazione del gioco
        public static void SaveProfile (ProfileData t_profile)
        {
            try
            {
                string path = Application.persistentDataPath + "/profile.dt";

                if(File.Exists(path)) File.Delete(path);

                FileStream file = File.Create(path);

                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(file, t_profile);
                file.Close();
                Debug.Log("Salvato");
            }
            catch
            {
                Debug.Log("Errore Salvataggio");
            }
            
        }

         //Funzione che permette di caricare le impostazioni di un account in un file che viene generato nella cartella di installazione del gioco
        public static ProfileData LoadProfile ()
        {
            ProfileData ret = new ProfileData();
            try
            {
                string path= Application.persistentDataPath+"/profile.dt";

                if(File.Exists(path))
                {
                    FileStream file= File.Open(path, FileMode.Open);
                    BinaryFormatter bf = new BinaryFormatter();
                    ret=(ProfileData) bf.Deserialize(file); 
                }
                Debug.Log("Carcicato");
            }
            
            catch
            {
                Debug.Log("Errore Caricamento");
            }
            

            return ret;
        }
    }
}


