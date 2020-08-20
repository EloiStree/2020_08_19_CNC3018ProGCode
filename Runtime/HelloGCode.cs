using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO;

public class HelloGCode : MonoBehaviour
{
    


    
    //https://all3dp.com/g-code-tutorial-3d-printer-gcode-commands/
        
    //public void SetFanOn(int index, float speedInPourcent) => SendCommand(string.Format("M106 P{0} S{1}", index, (int) (speedInPourcent*255)));
    //public void GetCurrentPosition() => SendCommand("M114");
    //public void GetGetFirmwareVersion() => SendCommand("M115");

        /*
    public void MoveClockwiseArc(Vector2 from, Vector2 to) => SendCommand(string.Format("G2 X{0} Y{1} I{2} J{3}", from.x, from.y, to.x, to.y));
    public void MoveCounterClockwiseArc(Vector2 from, Vector2 to) => SendCommand(string.Format("G3 X{0} Y{1} I{2} J{3}", from.x, from.y, to.x, to.y));


    public void G92_SetPosition(Vector3 newPosition, float extrude)
        => SendCommand(string.Format("G92 X{0} Y{1} Z{2} E{3}",
            newPosition.x, newPosition.y, newPosition.z, extrude));

    public void G92_2_ResetAxisOffsetToZero(Vector3 newPosition, float extrude)
    => SendCommand(string.Format("G92.2 "));

        */
        
}


[System.Serializable]
    public class GCodeManager
    {

    [Header("Configuration")]
    [SerializeField] public string portname = "COM15";
    [SerializeField] public string filename = null;
    [SerializeField] public int baudrate = 115200;
    [SerializeField] public int bufcount = 4; // nr of lines to buffer
    [SerializeField] public int timeout = 6000;
    [SerializeField] public long t1 = 0;
    [SerializeField] public bool realtime = false;
    [Header("Debug Private")]
        [SerializeField] Thread r; // reader thread
        [SerializeField] Semaphore sem;
        [SerializeField] public SerialPort port;
        [SerializeField] bool debug = false, log = false, progress = false, quit = false;
        [SerializeField] long t0 = 0;
        [SerializeField] TextWriter tw = null;
        [SerializeField] Stopwatch sw = Stopwatch.StartNew();
        [SerializeField] long msec() { return sw.ElapsedMilliseconds; }

        /*
         * Reader thread
         * Read data and find EOL chars
         * When one is found: the line is displayed
         */
        public  void Reader()
        {
            string s = "", n = "";
            long recnr = 0, t1 = 0;
            try
            {
                while (true)
                {
                    n += ((char)port.ReadChar()).ToString();
                    while (port.BytesToRead > 0)
                    {
                        n += port.ReadExisting();
                    }
                    while (n.Contains("\n"))
                    {
                        t1 = msec();
                        int p = n.IndexOf("\n");
                        s = n.Substring(0, p);
                        n = n.Substring(p + 1, n.Length - p - 1);
                        if (s.Length > 0)
                        {
                            if (tw != null)
                            {
                                tw.WriteLine((t1 - t0).ToString() + " " + s);
                                tw.Flush();
                            }
                            try
                            {
                                sem.Release(1);
                                recnr++;
                                if (debug) UnityEngine.Debug.Log((t1 - t0).ToString() + " " + recnr.ToString() + " < " + s);
                            }
                            catch
                            { // unexpected data?
                                if (debug) UnityEngine.Debug.Log((t1 - t0).ToString() + " " + recnr.ToString() + " << " + s);
                            }
                        }
                    }
                }
            }
            catch
            { // catch all, used for thread abort exception
                if (!quit)
                    UnityEngine.Debug.Log("Some error during read");
            }
        }

         void Help()
        {
            UnityEngine.Debug.Log(
              "\nsendg: Send GCODE file to your 3D printer via serial port. \n" +
              "(c) 2011 Peter Brier.\n" +
              "sendg is free software: you can redistribute it and/or modify it\n" +
              "under the terms of the GNU General Public License as published\n" +
              "by the Free Software Foundation\n" +
              "\nUSE: sendg -d -l -c[buffercount] -p[portname] -b[baudrate] [filename]\n\n" +
            " -r             Enable realtime process priority\n" +
            " -d             Enable debugging (show lots of debug messages)\n" +
            " -l             Enable logging (show time in msec, linenr and data)\n" +
              " -t             Set timeout [msec] to wait after last line is sent (default is 6000msec)\n" +
        " -e             Show built time estimation (in minutes)\n" +
              " -c[n]          Set delayed ack count (1 is no delayed ack, default is 4)\n" +
        " -p[name]       Specify portname (COMx on windows, /dev/ttyAMx on linux)\n" +
        " -b[baudrate]   Set baudrate (default is 115200)\n" +
        " [filename]     The GCODE file to send\n"
              );
        }
        
   
        public void InitaliazedConnectionAndThread()
        {
            
            
            if (debug)
            {
                UnityEngine.Debug.Log("Port: " + portname + " " + baudrate.ToString() + "bps");
                UnityEngine.Debug.Log("File: " + filename + " buffer:" + bufcount.ToString());
                UnityEngine.Debug.Log("Realtime priority: " + (realtime ? "ENABLED" : "DISABLED"));
            }

            if (realtime)
                using (Process p = Process.GetCurrentProcess())
                    p.PriorityClass = ProcessPriorityClass.RealTime;

            try
            {
                // open port and wait for Arduino to boot
                port = new SerialPort(portname, baudrate);
                port.Open();
                port.NewLine = "\n";
                port.DtrEnable = true;
                port.RtsEnable = true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log("SENDG: Cannot open serial port (Portname=" + portname + ")");
                if (debug)
                    UnityEngine.Debug.Log(ex.ToString());
                return;
            }
            Thread.Sleep(2000);

            // Init semaphore and Start 2nd thread
            sem = new Semaphore(0, bufcount);
            sem.Release(bufcount);
            r = new Thread(Reader);
            r.Start();
            t0 = msec();


            // Send all lines in the file
            string line;
            int linenr = 1;
            t0 = msec();
            StreamReader reader;
            try
            {
                reader = new StreamReader(filename);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log("SENDG: Cannot open file! (filename=" + filename + ")");
                if (debug) UnityEngine.Debug.Log(ex.ToString());
                r.Abort();
                return;
            }
            while ((line = reader.ReadLine()) != null)
            {
                string l = Regex.Replace(line, @"[;(]+.*[\n)]*", ""); // remove comment
                l = l.Trim();
                if (l.Length > 0)
                {
                    linenr++;
                    line = l + "\n";
                    sem.WaitOne();
                    port.Write(line);
                    // 20 min, 10%, total = 2min/%, total = 200 min
                    t1 = msec();
                    double time = (t1 - t0) / 60000.0; // elapsed time in minutes
                    double cur = (100.0 * reader.BaseStream.Position / (double)reader.BaseStream.Length); // current percentage
                    double total = 100.0 * (time / (double)cur); // remaining time in min
                    time = Math.Round(time);
                    total = Math.Round(total);
                    double remaining = total - time;
                    cur = Math.Floor(cur);
                    if (progress)
                    {
                        UnityEngine.Debug.Log(time.ToString() + "min: Line " + linenr.ToString() + " (" + cur.ToString() + "%) Remaining=" + remaining.ToString() + "min, Total=" + (total).ToString() + "min");
                    }
                    if (log)
                    {
                        UnityEngine.Debug.Log((t1 - t0).ToString() + " " + linenr.ToString() + " > " + line);
                    }
                }
            }
            tw.Close();
            // Wait for the last line to complete (1sec fixed time) and abort thread
            for (int i = 0; i < bufcount - 1; i++)
                sem.WaitOne();

            long e = msec() - t0;
            quit = true;
            Thread.Sleep(timeout);
            port.Close();
            r.Abort();
            UnityEngine.Debug.Log("Toal time: " + e.ToString() + " msec");
        }
    }