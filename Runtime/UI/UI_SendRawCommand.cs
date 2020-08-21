using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_SendRawCommand : MonoBehaviour
{

    public GRBL_CommandToSendBuffer m_sender;
    public string m_textToSend="";
    public void Push() {
        m_sender.AddCommandToSend(m_textToSend);
    }
    public void SetCommandToSend(string commande) {
        m_textToSend = commande;
    }
    

}
