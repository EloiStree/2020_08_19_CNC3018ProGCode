using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GRBL_TriggerFeedback : MonoBehaviour
{
    public GRBLConnectionMono m_connection;
    public float m_requestDeviceInfoStartDelay = 2f;
    public float m_requestStateInfoLoopDelay = 0.5f;
    public float m_requestParserInfoLoopDelay = 2f;

    private void Start()
    {
        Invoke("RequestGeneralInformation", m_requestDeviceInfoStartDelay);
        InvokeRepeating("RequestStateInformation", 0, m_requestStateInfoLoopDelay);
        InvokeRepeating("RequestParserInformation", 0, m_requestParserInfoLoopDelay);
    }
    public void RequestStateInformation()
    {
        m_connection.SendRawCommand(GCode3018Pro.RequestStateInfo());
    }
    public void RequestGeneralInformation()
    {
        m_connection.SendRawCommand(GCode3018Pro.RequestSettingInfo());
    }
    public void RequestParserInformation()
    {
        m_connection.SendRawCommand(GCode3018Pro.GRBL.ViewParserState());
    }

}
