using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_PadMoving : MonoBehaviour, IPointerDownHandler
{
    public GRBL_CommandToSendBuffer m_sender;
    public float m_widthInMm=150;
    public float m_depthInMm = 50;
    public MoveReference m_moveType;
    public float m_feedRate= 500;

    public RectTransform m_linkedTransform;
    public AspectRatioFitter m_aspectRatio;

    [Header("Debug")]
    public Vector2 m_pourcentClicked;


    private void OnValidate()
    {
        RefreshRatio();
    }

    private void RefreshRatio()
    {
        m_aspectRatio.aspectRatio = m_widthInMm / m_depthInMm;
    }

    public void SetWidth(string valueInMm)
    {
        m_widthInMm = float.Parse(valueInMm);
        RefreshRatio();
    }

    

    public void SetDepth(string valueInMm) {
        m_depthInMm = float.Parse(valueInMm);
        RefreshRatio();
    }
    public void SetFeedRate(string valueInMm) { m_feedRate = float.Parse(valueInMm);  }


    public void SetWidth(float valueInMm) { m_widthInMm = valueInMm; }
    public void SetDepth(float valueInMm) { m_depthInMm = valueInMm; }

    public void SetFeedRate(float valueInMm) { m_feedRate = valueInMm; }

    public void SetMoveType(int index) {
        if (index == 0)
            m_moveType = MoveReference.Relative;
        if (index == 1)
            m_moveType = MoveReference.Absolute;
    }


    
    public void OnPointerDown(PointerEventData eventData)
    {
        Vector2 v2;
        bool hasHit;
        CheckIfPadWasTouch(eventData, out hasHit, out v2);
       
            foreach (string  cmd in GCode3018Pro.Group.ControlledMove(
                    m_moveType, new GVector3(v2.x*m_widthInMm, v2.y*m_depthInMm, 0), m_feedRate))
            {
                m_sender.AddCommandToSend(cmd);
            }
        

    }

  

    void CheckIfPadWasTouch(PointerEventData dat, out bool hasBeenClick, out Vector2 whereInPourcent)
    {
        hasBeenClick = false;
        whereInPourcent = Vector3.zero;

        Vector2 localCursor;
        var rect1 = m_linkedTransform;
        if (rect1 == null) {

            return;
        }

        var pos1 = dat.position;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect1, pos1,
            null, out localCursor))
            return;
        

        int xpos = (int)(localCursor.x);
        int ypos = (int)(localCursor.y);

        if (xpos < 0) xpos = xpos + (int)rect1.rect.width / 2;
        else xpos += (int)rect1.rect.width / 2;

        if (ypos > 0) ypos = ypos + (int)rect1.rect.height / 2;
        else ypos += (int)rect1.rect.height / 2;

        m_pourcentClicked = localCursor;
        m_pourcentClicked.x = localCursor.x / (rect1.rect.width/2f);
        m_pourcentClicked.y = localCursor.y / (rect1.rect.height / 2f);
        hasBeenClick = true;
        whereInPourcent = m_pourcentClicked;
    }

   
}
