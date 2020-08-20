using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class GCodeConnectionMono : MonoBehaviour
{

    public AutoStartFromUnity m_autoStart;
    [System.Serializable]
    public class AutoStartFromUnity {
        public bool m_useAtStart;
        [SerializeField] public string m_portname = "COM15";
        [SerializeField] public int m_baudrate = 115200;
    }
    public GCodeConnection m_connection;

    [Header("Debug")]
    public string m_char;
    public string m_package;
    public string m_line;

    public string m_lastSent;
    public List<string> m_lastSentHistory = new List<string>();
    public List<string> m_recivedHistory = new List<string>();
    public string m_charHistory;
    //public UnityEvent m_previousMessageFail;
    //public UnityEvent m_previousMessageReceived;
    //public UnityEvent m_previousMessageSent;

    private void Awake()
    {
        if (m_autoStart.m_useAtStart) {
            StartConnection(m_autoStart.m_portname, m_autoStart.m_baudrate);
        }
    }

    public void StartConnectionWithDefaultCOM()
    {
        StartConnection(m_autoStart.m_portname, m_autoStart.m_baudrate);
    }
    public void StartConnection(string portname, int baudrate) {

        m_connection = new GCodeConnection(portname, baudrate);
        GCodeConnection.SetConnection(m_connection);
        m_connection.m_onReceivedChar += SetDebugChar;
        m_connection.m_onPackageReceived += SetDebugPackage;
        m_connection.m_onReceivedLineMessage+= SetDebugLine;
        m_connection.m_onCommandSent += RecordForDebug ;
    }

    private void RecordForDebug(string command)
    {
        m_lastSent = command;
        m_lastSentHistory.Insert(0, command);
        while(m_lastSentHistory.Count > 20) 
            m_lastSentHistory.RemoveAt(20);
    }

    private void SetDebugLine(string  received)
    {
        if (received != null && received.Length > 0) {
            m_line = received;
            m_recivedHistory.Insert(0, m_line);
            while (m_recivedHistory.Count > 20)
                m_recivedHistory.RemoveAt(20);
        }
    }

    private void SetDebugPackage(string received)
    {
        if(received!=null && received.Length>0)
             m_package = received;
    }

    private void SetDebugChar(char received)
    {
        m_char = received.ToString();
        m_charHistory += received;
        //if (m_char == "e")
        //    m_previousMessageFail.Invoke();
        //if (m_char == "o")
        //    m_previousMessageReceived.Invoke();
    }

    private void OnDestroy()
    {
        if (GCodeConnection.HasConnection()) {
            GCodeConnection.GetConnection().Stop();
        }

        m_connection.m_onReceivedChar -= SetDebugChar;
        m_connection.m_onPackageReceived -= SetDebugPackage;
        m_connection.m_onReceivedLineMessage -= SetDebugLine;
        m_connection.m_onCommandSent -= RecordForDebug;
    }

    public void SendRawCommand(string command) {
        m_connection.SendCommand(command);
       // m_previousMessageSent.Invoke();
    }
}

[System.Serializable]
public class GCodeConnection {

     static GCodeConnection m_inScene;

    public static void SetConnection(GCodeConnection connection) {
        m_inScene = connection;
    }
    internal static bool HasConnection()
    {
        return m_inScene != null;
    }

    internal static GCodeConnection GetConnection()
    {
        return m_inScene;
    }

    public delegate void OnSent(string command);
    public delegate void OnCharReturnReceived(char received);
    public delegate void OnStringReturnReceived(string received);

    [SerializeField] string m_portName="COM15";
    [SerializeField] int m_baudrate= 115200;

    SerialPort m_portLinked;
    Thread m_portListener;

    [SerializeField] bool m_isConnected;

    public OnSent m_onCommandSent;
    public OnCharReturnReceived m_onReceivedChar;
    public OnStringReturnReceived m_onReceivedLineMessage;
    public OnStringReturnReceived m_onPackageReceived;
    private bool m_requestToKillTheThread;

    public GCodeConnection(string portname, int baudrate =115200)
    {
        m_portName = portname;
        m_baudrate = baudrate;
        Refresh();
    }

    public SerialPort GetUsedPort() {
        return m_portLinked;
    }

    private void Refresh()
    {
        m_isConnected = false;
            Stop();
        try
        {
            // open port and wait for Arduino to boot
            m_portLinked = new SerialPort(m_portName, m_baudrate);
            m_portLinked.Open();
            m_portLinked.NewLine = "\n";
            m_portLinked.DtrEnable = true;
            m_portLinked.RtsEnable = true;
            m_isConnected = true;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning("SENDG: Cannot open serial port (Portname=" + m_portName + ")");
            return;
        }

        m_requestToKillTheThread = false;
        m_portListener = new Thread(ListenToMessageReceived);
        m_portListener.Start();

    }

    private void ListenToMessageReceived()
    {
        

        string endOfLineReceived = "", receivedInPackages = "", lastPackage = "" ;
        try
        {
            while (!m_requestToKillTheThread && m_portLinked!=null && m_portLinked.IsOpen)
            {
                lastPackage = "";
                char c = (char)m_portLinked.ReadChar();
                receivedInPackages += c;

                if (m_onReceivedChar != null)
                    m_onReceivedChar(c);

                while (m_portLinked.BytesToRead > 0)
                {
                    receivedInPackages += m_portLinked.ReadExisting();
                }
                if (receivedInPackages.Contains("\n")){
                    while (receivedInPackages.Contains("\n"))
                    {
                        int p = receivedInPackages.IndexOf("\n");
                        endOfLineReceived = receivedInPackages.Substring(0, p);
                        receivedInPackages = receivedInPackages.Substring(p + 1, receivedInPackages.Length - p - 1);
                        if (endOfLineReceived.Length > 0)
                        {
                            m_onReceivedLineMessage(endOfLineReceived);

                            if (m_onReceivedLineMessage != null)
                                m_onReceivedLineMessage(lastPackage);

                        }
                    }
                }
                lastPackage = receivedInPackages;
                if (m_onPackageReceived != null)
                    m_onPackageReceived(lastPackage);

            }
        }
        catch(Exception e)
        {
            UnityEngine.Debug.LogError("Some error during read"+e.StackTrace);
            m_isConnected = false;
        }

    }

    public void SendCommand(string cmd)
    {
        m_portLinked.Write(cmd + "\n");
        if(m_onCommandSent!=null)
            m_onCommandSent(cmd);
    }


    public void Stop() {
        if (m_portLinked != null)
            m_portLinked.Close();
        if (m_portListener != null)
            m_portListener.Abort();
        m_requestToKillTheThread = true;
    }

    public void Flush()
    {
        Refresh();
    }
}
public enum ReturnReceived { Valide, Error}
