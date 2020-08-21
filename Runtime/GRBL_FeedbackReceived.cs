using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GRBL_FeedbackReceived : MonoBehaviour
{
    
    public GRBLInformation m_globalStatus = new GRBLInformation();
    public GRBLStatusEvent m_onStatusRefresh;
    public GRBLInformationEvent m_onInformationRefresh;
    public GRBLErrorEvent m_onErrorOccured;


    [Header("Last")]
    public string m_lastReceived;
    public string m_firmwareInfo;
    public string m_lastParserState;

    public GRBLState GetDeviceState()
    {
        return m_globalStatus.m_status.m_currentState;
    }

    public string m_lastParserInformation;
    public string m_lastSettingInformation;
    public string m_lastError;


    [Header("History")]
    public List<string> m_received;
    public int m_historySize = 30;




    public void TranslateTheLineFromGRBL(string line) {
        line = line.Trim();
        if (line == null || line.Length <= 0)
            return;
        m_lastReceived = line;
        m_received.Insert(0, line);
        while (m_received.Count > m_historySize)
            m_received.RemoveAt(m_historySize);


        if (line.ToLower().IndexOf("grbl") == 0) {
            m_firmwareInfo = line;
            int endIndex= line.IndexOf("[");
            if (endIndex > -1)
                m_firmwareInfo = m_firmwareInfo.Substring(0,endIndex);
            m_globalStatus.m_firmware = m_firmwareInfo.Trim();
            m_onInformationRefresh.Invoke(m_globalStatus);
        }
        if (line.ToLower().IndexOf("error") == 0)
        {
            m_lastError = line;
            int errorId;
            if (int.TryParse(line.ToLower().Replace("error:", ""), out errorId)){
                m_onErrorOccured.Invoke(new GRBLError(errorId));
            }
        }
        if (line.ToLower().IndexOf("ok") == 0)
        {
        }
        if (line.ToLower().IndexOf("$") == 0) {
            m_lastSettingInformation = line;
            string[] tokens = line.Replace("$", "").Split('=');
            if (tokens.Length == 2) {
                string key=tokens[0];
                string value = tokens[1] ;
                m_globalStatus.m_setting.Set(key, value);
            }

            m_onInformationRefresh.Invoke(m_globalStatus);

        }
        if (line.ToLower().IndexOf("[gc:") == 0) {
            m_lastParserInformation = line;
            m_onInformationRefresh.Invoke(m_globalStatus);

            ExtractInformationOf(line,ref m_globalStatus);


        }
        if (line.Length > 1 && line[0] == '<' && line[line.Length - 1] == '>') {
            m_lastParserState = line;
            string temp = line.Substring(1, line.Length - 2);
            string[] tokens = temp.Split('|');
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (CheckAndSoSetState(token, ref m_globalStatus.m_status)) { }
                else if (CheckAndSoSetPosition(token, ref m_globalStatus.m_status)) { }
                else if (CheckAndSoSetFeedSpeed(token, ref m_globalStatus.m_status)) { }


            }

            m_onStatusRefresh.Invoke(m_globalStatus.m_status);
        }
        //[GC:G0 G54 G17 G21 G90 G94 M5 M9 T0 F0 S0]
        //<Idle|MPos:0.000,0.000,0.000|FS:0,0>
        //<Idle|MPos:0.000,0.000,0.000|FS:0,0|OV:100,100,100>
        // <Idle|MPos:134.826,81.991,0.000|FS:0,0|WCO:0.000,30.000,0.000>
        //error: 8

        

    }

    private void ExtractInformationOf(string gcValues, ref GRBLInformation status)
    {
        gcValues = gcValues.ToUpper().Replace("[GC:", "").Replace("]", "");
        string[] tokens = gcValues.Split(' ');
        foreach (string value in tokens)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 1)
                continue;
            bool hasBeenFound = false;
            if (!hasBeenFound)
                status.m_motion = GetStatusOf(value, status.m_motion, out hasBeenFound);
             if (!hasBeenFound)
                status.m_track = GetStatusOf(value, status.m_track, out hasBeenFound);
             if (!hasBeenFound)
                status.m_plane = GetStatusOf(value, status.m_plane, out hasBeenFound);
             if (!hasBeenFound)
                status.m_motion = GetStatusOf(value, status.m_motion, out hasBeenFound);
             if (!hasBeenFound)
                status.m_distance = GetStatusOf(value, status.m_distance, out hasBeenFound);
             if (!hasBeenFound)
                status.m_arcdistance = GetStatusOf(value, status.m_arcdistance, out hasBeenFound);
             if (!hasBeenFound)
                status.m_feedRate = GetStatusOf(value, status.m_feedRate, out hasBeenFound);
             if (!hasBeenFound)
                status.m_units = GetStatusOf(value, status.m_units, out hasBeenFound);
             if (!hasBeenFound)
                status.m_cutterRadius = GetStatusOf(value, status.m_cutterRadius, out hasBeenFound);
             if (!hasBeenFound)
                status.m_toolLengthOffset = GetStatusOf(value, status.m_toolLengthOffset, out hasBeenFound);
             if (!hasBeenFound)
                status.m_cutterRadius = GetStatusOf(value, status.m_cutterRadius, out hasBeenFound);
             if (!hasBeenFound)
                status.m_program = GetStatusOf(value, status.m_program, out hasBeenFound);
             if (!hasBeenFound)
                status.m_spindleState = GetStatusOf(value, status.m_spindleState, out hasBeenFound);
             if (!hasBeenFound)
                status.m_coolantState = GetStatusOf(value, status.m_coolantState, out hasBeenFound);

            
        }
    }

    private  T GetStatusOf<T>(string value, T previousValue,out bool found) where T : struct
    {
        found = false;
        value = value.Replace(".", "_").Trim();
        foreach (T item in GetEnumValues<T>())
        {
            if (item.ToString().ToUpper() == value.ToUpper())
            {
                found = true;
                return item;
            }

        }
        return previousValue;
        
    }

    public static T[] GetEnumValues<T>() where T : struct
    {
        if (!typeof(T).IsEnum)
        {
            throw new ArgumentException("GetValues<T> can only be called for types derived from System.Enum", "T");
        }
        return (T[])Enum.GetValues(typeof(T));
    }

    private bool CheckAndSoSetFeedSpeed(string token, ref GRBLRealtimeStatus status)
    {
        token = token.ToLower();
        if (token.IndexOf("fs:") == 0) {
            string[] t = token.Substring(3).Split(',');
            int realtimeSpeedRate, spindleSpeed;
            if (t.Length >1 && int.TryParse(t[0], out realtimeSpeedRate) &&
                int.TryParse(t[1], out spindleSpeed))
            {
                status.m_realtimeFeedRate = realtimeSpeedRate;
                status.m_spindleSpeed = spindleSpeed;
                return true;
            }

        }
        else if (token.IndexOf("f:")==0) {
            int realtimefeedrate = 0;
            if (int.TryParse(token.Substring(2), out realtimefeedrate)) {
                status.m_realtimeFeedRate = realtimefeedrate;

                return true;
            }
        }

        return false;
    }

    private bool CheckAndSoSetPosition(string token, ref GRBLRealtimeStatus status)
    {
        try
        {

            token = token.ToLower();
            float x, y, z;
            if (RecoverVector3Value(token, "mpos:", out x, out y, out z))
            {
                status.m_motorPosition.x = x;
                status.m_motorPosition.y = y;
                status.m_motorPosition.z = z;

                status.m_workspacePosition.x = x - status.m_workCoordinateOffset.x;
                status.m_workspacePosition.y = y - status.m_workCoordinateOffset.y;
                status.m_workspacePosition.z = z - status.m_workCoordinateOffset.z;
                return true;
            }
            else if (RecoverVector3Value(token, "wco:", out x, out y, out z))
            {
                status.m_workCoordinateOffset.x = x;
                status.m_workCoordinateOffset.y = y;
                status.m_workCoordinateOffset.z = z;
                return true;
            }
            else if (RecoverVector3Value(token, "ov:", out x, out y, out z))
            {
                status.m_overrideValueInPourcent.x = x;
                status.m_overrideValueInPourcent.y = y;
                status.m_overrideValueInPourcent.z = z;
                return true;
            }
            else if (RecoverVector3Value(token, "wpos:", out x, out y, out z))
            {
                status.m_workspacePosition.x = x;
                status.m_workspacePosition.y = y;
                status.m_workspacePosition.z = z;
                status.m_motorPosition.x = x + status.m_workCoordinateOffset.x;
                status.m_motorPosition.y = y + status.m_workCoordinateOffset.y;
                status.m_motorPosition.z = z + status.m_workCoordinateOffset.z;

                return true;
            }
        }
        catch { return false; }
        return false;
    }

    private bool RecoverVector3Value(string value, string startWith, out float x, out float y, out float z)
    {
        value = value.ToLower();
        x = 0;
        y = 0;
        z = 0;
        if (value.IndexOf(startWith) == 0)
        {
            string[] tokens = value.Substring(startWith.Length).Split(',');
            if (tokens.Length == 3)
            {
                 x = float.Parse(tokens[0].Replace(".", ","));
                 y = float.Parse(tokens[1].Replace(".", ","));
                 z = float.Parse(tokens[2].Replace(".", ","));
                return true;
            }
        }
        return false;
    }

    private bool CheckAndSoSetState(string token, ref GRBLRealtimeStatus status)
    {
        token = token.ToLower();
        if (token == "idle")
        { status.m_currentState = GRBLState.Idle; return true; }
        if (token == "run")
        { status.m_currentState = GRBLState.Run; return true; }
        if (token == "hold")
        { status.m_currentState = GRBLState.Hold; return true; }
        if (token == "job")
        { status.m_currentState = GRBLState.Jog; return true; }
        if (token == "alarm")
        { status.m_currentState = GRBLState.Alarm; return true; }
        if (token == "door")
        { status.m_currentState = GRBLState.Door; return true; }
        if (token == "check")
        { status.m_currentState = GRBLState.Check; return true; }
        if (token == "home")
        { status.m_currentState = GRBLState.Home; return true; }
        if (token == "sleep")
        { status.m_currentState = GRBLState.Sleep; return true; }
        return false;
    }


}

[System.Serializable]
public class GRBLInformationEvent : UnityEvent<GRBLInformation>
{

}
[System.Serializable]
public class GRBLStatusEvent : UnityEvent<GRBLRealtimeStatus>
{

}
[System.Serializable]
public class GRBLErrorEvent : UnityEvent<GRBLError>
{

}

public class GRBLError {
    public GRBLError(int id) {
        m_id = id;
    }
    public int m_id;
    public string GetDescription() { return GetDescriptionOf(m_id); }
    public static string GetDescriptionOf(int idError) {
        
        if(idError==1 )  return "G - code words consist of a letter and a value. Letter was not found.";
        if (idError==2 )  return "Numeric value format is not valid or missing an expected value.";
        if (idError==3 )  return "Grbl '$' system command was not recognized or supported.";
        if (idError==4 )  return "Negative value received for an expected positive value.";
        if (idError==5 )  return "Homing cycle is not enabled via settings.";
        if (idError==6 )  return "Minimum step pulse time must be greater than 3usec";
        if (idError==7 )  return "EEPROM read failed.Reset and restored to default values.";
        if (idError==8 )  return "Grbl '$' command cannot be used unless Grbl is IDLE.Ensures smooth operation during a job.";
        if (idError==9 )  return "G - code locked out during alarm or jog state";
        if (idError==10)  return "Soft limits cannot be enabled without homing also enabled.";
        if (idError==11)  return "Max characters per line exceeded.Line was not processed and executed.";
        if (idError==12)  return "(Compile Option) Grbl '$' setting value exceeds the maximum step rate supported.";
        if (idError==13)  return "Safety door detected as opened and door state initiated.";
        if (idError==14)  return "(Grbl - Mega Only) Build info or startup line exceeded EEPROM line length limit.";
        if (idError==15)  return "Jog target exceeds machine travel.Command ignored.";
        if (idError==16)  return "Jog command with no '=' or contains prohibited g - code.";
        if (idError==17)  return "Laser mode requires PWM output.";
        if (idError==20)  return "Unsupported or invalid g - code command found in block.";
        if(idError==21)  return "More than one g - code command from same modal group found in block.";
        if (idError==22)  return "Feed rate has not yet been set or is undefined.";
        if (idError==23)  return "G - code command in block requires an integer value.";
        if (idError==24)  return "Two G - code commands that both require the use of the XYZ axis words were detected in the block.";
        if (idError==25)  return "A G - code word was repeated in the block.";
        if (idError==26)  return "A G - code command implicitly or explicitly requires XYZ axis words in the block, but none were detected.";
        if (idError==27)  return "N line number value is not within the valid range of 1 - 9, 999, 999.";
        if (idError==28)  return "A G - code command was sent, but is missing some required P or L value words in the line.";
        if (idError==29)  return "Grbl supports six work coordinate systems G54 - G59.G59.1, G59.2, and G59.3 are not supported.";
        if (idError==30)  return "The G53 G - code command requires either a G0 seek or G1 feed motion mode to be active.A different motion was active.";
        if (idError==31)  return "There are unused axis words in the block and G80 motion mode cancel is active.";
        if (idError==32)  return "A G2 or G3 arc was commanded but there are no XYZ axis words in the selected plane to trace the arc.";
        if (idError==33)  return "The motion command has an invalid target.G2, G3, and G38.2 generates this error, if the arc is impossible to generate or if the probe target is the current position.";
        if (idError==34)  return "A G2 or G3 arc, traced with the radius definition, had a mathematical error when computing the arc geometry.Try either breaking up the arc into semi-circles or quadrants, or redefine them with the arc offset definition.";
        if (idError==35)  return "A G2 or G3 arc, traced with the offset definition, is missing the IJK offset word in the selected plane to trace the arc.";
        if (idError==36)  return "There are unused, leftover G-code words that aren't used by any command in the block.";
        if (idError==37)  return "The G43.1 dynamic tool length offset command cannot apply an offset to an axis other than its configured axis.The Grbl default axis is the Z - axis.";
        if (idError == 38) return "Tool number greater than max supported value.";
        return "No information of the error:"+idError;
    }
}

public enum GRBLState{Undefined, Idle, Run, Hold, Jog, Alarm, Door, Check, Home, Sleep};

[System.Serializable]
public class GRBLInformation {

    public string m_firmware;
    public GRBLRealtimeStatus m_status = new GRBLRealtimeStatus();
    public  ParserMotion            m_motion;
    public  ParserTrack             m_track;
    public  ParserPlaneSelected     m_plane;
    public  ParserDistance          m_distance;
    public  ParserArcDistance       m_arcdistance;
    public  ParserFeedRate          m_feedRate;
    public  ParserUnits             m_units;
    public  ParseCutterRadius       m_cutterRadius;
    public  ParserToolLenghtOffset  m_toolLengthOffset;
    public  ParserProgramMode       m_program;
    public  ParserSpindleState      m_spindleState;
    public  ParserCoolantState      m_coolantState;    
    public GRBLSetting m_setting = new GRBLSetting();

}
[System.Serializable]
public class GRBLRealtimeStatus {
    public float m_realtimeFeedRate;
    public int m_spindleSpeed;
    public Vector3 m_motorPosition;
    public Vector3 m_workspacePosition;
    public Vector3 m_overrideValueInPourcent;
    public Vector3 m_workCoordinateOffset;
    public GRBLState m_currentState = GRBLState.Undefined;
}

[System.Serializable]
public class GRBLSetting
{

    public void Set(string key, string value) {

        try {
            switch (key)
            {
                case "0": _0stepPulseMicroseconds = int.Parse(value); return;
                case "1": _1stepidledelaymilliseconds = int.Parse(value); return;
                case "2": _2stepportinvertmask = int.Parse(value); return;
                case "3":   _3directionportinvertmask = int.Parse(value); return;
                case "4":   _4stepenableinvert = "1" == (value); return;
                case "5":   _5limitpinsinvert =  "1"==(value); return;
                case "6":   _6probepininvert = "1" == (value); return;
                case "10":  _10statusreportmask = int.Parse(value); return;
                case "11":  _11Junctiondeviationmm = float.Parse(value); return;
                case "12":  _12arctolerancemm = float.Parse(value); return;
                case "13":  _13reportinches = "1" == (value); return;
                case "20":  _20softlimits = "1" == (value); return;
                case "21":  _21hardlimits = "1" == (value); return;
                case "22":  _22homingcycle = "1" == (value); return;
                case "23":  _23homingdirinvertmask = int.Parse(value); return;
                case "24":  _24homingfeedmm_min = float.Parse(value); return;
                case "25":  _25homingseekmm_min = float.Parse(value); return;
                case "26":  _26homingdebouncemilliseconds = int.Parse(value); return;
                case "27":  _27homingpulloffmm = float.Parse(value); return;
                case "30":  _30maxspindlespeedRPM = int.Parse(value); return;
                case "31":  _31minspindlespeedRPM = int.Parse(value); return;
                case "32":  _32lasermode = "1"==value; return;
                case "101": _101Ysteps_mm = float.Parse(value); return;
                case "102": _102Zsteps_mm = float.Parse(value); return;
                case "110": _110XMaxratemm_min = float.Parse(value); return;
                case "111": _111YMaxratemm_min = float.Parse(value); return;
                case "112": _112ZMaxratemm_min = float.Parse(value); return;
                case "120": _120XAccelerationmm_sec2 = float.Parse(value); return;
                case "121": _121YAccelerationmm_sec2 = float.Parse(value); return;
                case "122": _122ZAccelerationmm_sec2 = float.Parse(value); return;
                case "130": _130XMaxtravelmm = float.Parse(value); return;
                case "131": _131YMaxtravelmm = float.Parse(value); return;
                case "132": _132ZMaxtravelmm = float.Parse(value); return;
            }

        }
        catch (Exception e) { }
    }

public int _0stepPulseMicroseconds         ;
public int _1stepidledelaymilliseconds    ;
public int  _2stepportinvertmask           ;
public int _3directionportinvertmask      ;
public bool  _4stepenableinvert      ;
public bool _5limitpinsinvert       ;
public bool _6probepininvert        ;
public int  _10statusreportmask            ;
public float  _11Junctiondeviationmm         ;
public float  _12arctolerancemm              ;
public bool _13reportinches         ;
public bool _20softlimits           ;
public bool _21hardlimits           ;
public bool _22homingcycle          ;
public int _23homingdirinvertmask         ;
public float  _24homingfeedmm_min            ;
public float  _25homingseekmm_min            ;
public int _26homingdebouncemilliseconds  ;
public float  _27homingpulloffmm             ;
public int _30maxspindlespeedRPM          ;
public int _31minspindlespeedRPM          ;
public bool  _32lasermode            ;
public float  _100Xsteps_mm                  ;
public float  _101Ysteps_mm                  ;
public float  _102Zsteps_mm                  ;
public float  _110XMaxratemm_min             ;
public float  _111YMaxratemm_min             ;
public float  _112ZMaxratemm_min             ;
public float  _120XAccelerationmm_sec2       ;
public float  _121YAccelerationmm_sec2       ;
public float  _122ZAccelerationmm_sec2       ;
public float  _130XMaxtravelmm               ;
public float  _131YMaxtravelmm               ;
public float _132ZMaxtravelmm                ;

}

public enum ParserMotion            { Undefined, G0, G1, G2, G3, G38_2, G38_3, G38_4, G38_5, G80 }
public enum ParserTrack             { Undefined, G54, G55, G56, G57, G58, G59 }
public enum ParserPlaneSelected     { Undefined, G17, G18, G19 }
public enum ParserDistance          { Undefined, G90, G91 }
public enum ParserArcDistance       { Undefined, G91_1 }
public enum ParserFeedRate          { Undefined, G93, G94 }
public enum ParserUnits             { Undefined, G20, G21 }
public enum ParseCutterRadius       { Undefined, G40 }
public enum ParserToolLenghtOffset  { Undefined, G43_1, G49 }
public enum ParserProgramMode       { Undefined, M0, M1, M2, M30 }
public enum ParserSpindleState      { Undefined, M3, M4, M5 }
public enum ParserCoolantState      { Undefined, M7, M8, M9 }