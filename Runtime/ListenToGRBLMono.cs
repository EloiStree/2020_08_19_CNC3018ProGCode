using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ListenToGRBLMono : MonoBehaviour
{
    public GRBLConnectionMono m_linkedConnection;
    public GRBLMessageLine m_onGrblLineReceived;
    public GRBLMessageLine m_onSentLineToGrbl;
    [Header("Debug")]
    public int m_historyLength=30;
    public string m_lastSent;
    public List<string> m_lastSentHistory = new List<string>();
    public string m_lastReceived;
    public List<string> m_recivedHistory = new List<string>();



    public Queue<string> m_newLinesReceived = new Queue<string>();
    public Queue<string> m_linesSent = new Queue<string>();

    private void Start()
    {

        if (m_linkedConnection != null)
        {
            m_linkedConnection.m_connection.m_onReceivedLineMessage += SetDebugLine;
            m_linkedConnection.m_connection.m_onCommandSent += RecordForDebug;
        }
    }

    private void Update()
    {
        if (m_newLinesReceived.Count > 0)
        {
            m_onGrblLineReceived.Invoke(m_newLinesReceived.Dequeue());
        }
        if (m_linesSent.Count > 0)
        {
            m_onSentLineToGrbl.Invoke(m_linesSent.Dequeue());
        }
    }

    private void RecordForDebug(string command)
    {
        m_lastSent = command;
        m_linesSent.Enqueue(command);
        m_lastSentHistory.Insert(0, command);
        while (m_lastSentHistory.Count > m_historyLength)
            m_lastSentHistory.RemoveAt(m_historyLength);
    }

    private void SetDebugLine(string received)
    {
        if (received != null && received.Length > 0)
        {
            m_newLinesReceived.Enqueue(received);
            m_lastReceived = received;
            m_recivedHistory.Insert(0, m_lastReceived);
            while (m_recivedHistory.Count > m_historyLength)
                m_recivedHistory.RemoveAt(m_historyLength);
        }
    }


    private void OnDestroy()
    {
        if (m_linkedConnection != null) {
            m_linkedConnection.m_connection.m_onReceivedLineMessage -= SetDebugLine;
            m_linkedConnection.m_connection.m_onCommandSent -= RecordForDebug;
        }
    }

   
}

[System.Serializable]
public class GRBLMessageLine : UnityEvent<string> { }