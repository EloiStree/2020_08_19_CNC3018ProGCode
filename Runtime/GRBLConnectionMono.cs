using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class GRBLConnectionMono : MonoBehaviour
{

    public AutoStartFromUnity m_autoStart;
    [System.Serializable]
    public class AutoStartFromUnity {
        public bool m_useAtStart;
        [SerializeField] public string m_portname = "COM15";
        [SerializeField] public int m_baudrate = 115200;
    }
    public GRBLConnection m_connection;

    [Header("Debug")]
    public string m_line;
    public string m_lastSent;
    public Queue<string> m_newLinesReceived = new Queue<string>();
    public GRBLMessageLine m_onGrblLineReceived;
    private void Awake()
    {
        if (m_autoStart.m_useAtStart) {
            StartConnection(m_autoStart.m_portname, m_autoStart.m_baudrate);
        }
    }
    private void Update()
    {
        if (m_newLinesReceived.Count > 0)
        {
            m_onGrblLineReceived.Invoke(m_newLinesReceived.Dequeue());
        }
       
    }


    public void StartConnectionWithDefaultCOM()
    {
        StartConnection(m_autoStart.m_portname, m_autoStart.m_baudrate);
    }
    public void StartConnection(string portname, int baudrate) {

        m_connection = new GRBLConnection(portname, baudrate);
        GRBLConnection.SetConnection(m_connection);
        m_connection.m_onCommandSent += RecordForDebug ;
        m_connection.m_onReceivedLineMessage += ReceivedLine;

    }

    private void ReceivedLine(string received)
    {
        m_newLinesReceived.Enqueue(received);
    }

    private void RecordForDebug(string command)
    {
        m_lastSent = command;
    }

   
    
    private void OnDestroy()
    {
        if (GRBLConnection.HasConnection()) {
            GRBLConnection.GetConnection().Stop();
        }
        m_connection.m_onCommandSent -= RecordForDebug;
        m_connection.m_onReceivedLineMessage -= ReceivedLine;
    }

    public void SendRawCommand(string command) {
        m_connection.SendCommand(command);
    }
}

[System.Serializable]
public class GRBLConnection {

     static GRBLConnection m_inScene;

    public static void SetConnection(GRBLConnection connection) {
        m_inScene = connection;
    }
    internal static bool HasConnection()
    {
        return m_inScene != null;
    }

    internal static GRBLConnection GetConnection()
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

    public GRBLConnection(string portname, int baudrate =115200)
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

    public void PauseDevice()
    {
        GRBLConnection.TryToSendCommand(GCode3018Pro.PauseDeviceJobs());
    }
    public void ResumeDevice()
    {

        GRBLConnection.TryToSendCommand(GCode3018Pro.ResumeDeviceJobs());
    }
   


    public static void TryToSendCommand(string command)
    {
        if (HasConnection())
        {
            GRBLConnection connection = GetConnection();
            connection.SendCommand(command);
        }
    }
}
public enum ReturnReceived { Valide, Error}
