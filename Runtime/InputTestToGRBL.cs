using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputTestToGRBL : MonoBehaviour
{

    public GRBL_CommandToSendBuffer m_sender;
    public Vector2 bedDirection;
    public float m_incrementInMM = 5;
    public MoveReference m_moveType = MoveReference.Relative;

    void Update()
    {
        Vector2 bedDirectionPrevious = bedDirection;
        bedDirection.x = Input.GetAxis("Horizontal");
        bedDirection.y = Input.GetAxis("Vertical");

        if (bedDirectionPrevious == Vector2.zero && bedDirection != bedDirectionPrevious) {
        m_sender.SendCommandDirectly(
            m_moveType == MoveReference.Relative ?
            GCode3018Pro.AsRelative() :
            GCode3018Pro.AsAbsolute());
        }
        if (bedDirection != Vector2.zero) {
            m_sender.OverrideAllToPush(
                GCode3018Pro.FastMove( 
                    new GVector3(
                        bedDirection.x*m_incrementInMM,
                        bedDirection.y*m_incrementInMM,
                        0)));
        }

    }
}
