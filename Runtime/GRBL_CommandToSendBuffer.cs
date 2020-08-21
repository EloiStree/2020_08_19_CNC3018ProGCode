using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GRBL_CommandToSendBuffer : MonoBehaviour
{

    public GRBLConnectionMono m_grblSend;
    public GRBL_FeedbackReceived m_grblReceived;

    public int m_inQueue;

    public Queue<string> m_commandsToSend = new Queue<string>();
    public Queue<string> m_commandsToSendAsSoonAsPossible = new Queue<string>();

    public void OverrideAllToPush(string cmd)
    {
        m_commandsToSend.Clear();
        m_commandsToSendAsSoonAsPossible.Clear();
        AddCommandToSendAsSoonAsPossible(cmd);
    }

    public void AddCommandToSend (string command){
        m_commandsToSend.Enqueue(command);
    }
    public void SendCommandDirectly(string command)
    {
        m_grblSend.SendRawCommand(command);
    }
    public void AddCommandToSendAsSoonAsPossible(string command)
    {
        m_commandsToSendAsSoonAsPossible.Enqueue(command);
    }

    public void FlushAllCommands() {
        m_commandsToSend.Clear();
    }

    public string m_lastSend;
    
    public GRBLState m_previousState;
    public GRBLState m_currentState;

    public float m_sendReceivedDelay=0.2f;
    public float m_senderCooldown;

    public void Update()
    {
        m_inQueue = m_commandsToSend.Count + m_commandsToSendAsSoonAsPossible.Count;
        if(m_senderCooldown > 0)
            m_senderCooldown -= Time.deltaTime;
        

        //Try to find a way to send the command then to wait the device to effectively run before the next one.
        {
            m_currentState = m_grblReceived.GetDeviceState();
            if (m_previousState != m_currentState && m_currentState == GRBLState.Idle) {
                m_senderCooldown = 0;
            }
            m_previousState = m_currentState;
        }

        if (m_senderCooldown > 0)
            return;
        if (m_currentState != GRBLState.Idle)
            return;


        if (m_commandsToSendAsSoonAsPossible.Count > 0)
        {
            m_lastSend = m_commandsToSendAsSoonAsPossible.Dequeue();
            m_grblSend.SendRawCommand(m_lastSend);
            m_senderCooldown = m_sendReceivedDelay;
        }
        else if (m_commandsToSend.Count > 0) {

            m_lastSend = m_commandsToSend.Dequeue();
            m_grblSend.SendRawCommand(m_lastSend);
            m_senderCooldown = m_sendReceivedDelay;
        }
    }
}
