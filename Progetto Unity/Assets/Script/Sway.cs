using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.Colloquio.SimpleHostile
{

    public class Sway : MonoBehaviourPunCallbacks
    {

        #region Variable

        public float intesity;
        public float smooth;
        public bool IsMine;
        private Quaternion origin_rotation;

        #endregion

        #region MonoBehaviour Callbacks
        // Start is called before the first frame update
        void Start()
        {
            origin_rotation = transform.localRotation;
        }

        // Update is called once per frame
        private void Update()
        {
            if(Pause.paused) return;
            UpdateSway();
        }
        #endregion

        #region Private Methods

        private void UpdateSway()
        {
            //Controls
            float Horizontal= Input.GetAxis("Mouse X");
            float Vertical = Input.GetAxis("Mouse Y");

            if(!IsMine)
            {
                Horizontal= 0;
                Vertical =0;
            }

            //Calculate target rotation
            Quaternion t_x_adj = Quaternion.AngleAxis(intesity * Horizontal, Vector3.down);
            Quaternion t_y_adj = Quaternion.AngleAxis(intesity * Vertical, Vector3.right);
            Quaternion target_rotation = origin_rotation * t_x_adj * t_y_adj;

            transform.localRotation = Quaternion.Lerp(transform.localRotation, target_rotation, Time.deltaTime * smooth);
            
        }

        #endregion
    }
}
