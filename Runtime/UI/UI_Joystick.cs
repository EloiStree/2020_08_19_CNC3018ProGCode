using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UI_Joystick : MonoBehaviour
{

    public bool m_clockwise=true;
    public bool m_moveFast=true;
    public float m_speedRateInMm=500;
    public float m_rotationSpeed = 1000;

    private bool m_isMotorTheoriclyOn;

    public MoveReference m_moveReference = MoveReference.Relative;

    private void Start()
    {
        foreach (string cmd in GCode3018Pro.Group.ClassicStart())
        {
            SendCommandIfConnection(cmd);
        }
    }

    public void SendCommandIfConnection(string cmd)
    {

        if (GCodeConnection.HasConnection())
        {
            GCodeConnection connection = GCodeConnection.GetConnection();
            connection.SendCommand(cmd);
        }
    }


    public void SaveAsWorkspace() {
        SendCommandIfConnection(GCode3018Pro.SaveWorkspace());
    }
    public void SaveAsHome() {

        SendCommandIfConnection(GCode3018Pro.SetHomeWithCurrent());
    }

    public void GoToHome()
    {
        SendCommandIfConnection(GCode3018Pro.GoHome());
    }
    public void GoZero()
    {
        MoveInDirection(GVector3.Zero());
    }




    public void SetFeedRate(float feedRateInMM)
    {
        m_speedRateInMm = 500;
    }
    public void SetRotationSpeed(float rotationPerMinutes)
    {
        m_rotationSpeed = rotationPerMinutes;
        SendCommandIfConnection(
            GCode3018Pro.RotationSpeed(rotationPerMinutes)
            );
    }

    public void SetAsRelative(bool isRelative) {
        m_moveReference = isRelative ? MoveReference.Relative : MoveReference.Absolute;
    }

    public void SetMotor(bool onOff)
    {
        m_isMotorTheoriclyOn = onOff;

        if (onOff)
            SendCommandIfConnection(
                m_clockwise ? GCode3018Pro.StartMotorClockwise() :
                GCode3018Pro.StartMotorCounterClockwise());
        else SendCommandIfConnection(GCode3018Pro.StopMotor());
        SetRotationSpeed(m_rotationSpeed);
    }

    public void X(float valueInMM)
    {
        MoveInDirection(GVector3.Width(valueInMM));

    }
    
    public void Y(float valueInMM)
    {

        MoveInDirection(GVector3.Depth(valueInMM));
    }
    public void Z(float valueInMM)
    {

        MoveInDirection(GVector3.Height(valueInMM));
    }

    public void RX(float valueInMM)
    {
        m_moveReference = MoveReference.Relative;
        X(valueInMM);
    }
    public void RY(float valueInMM)
    {
        m_moveReference = MoveReference.Relative;
        Y(valueInMM);
    }
    public void RZ(float valueInMM)
    {
        m_moveReference = MoveReference.Relative;
        Z(valueInMM);
    }
    public void AX(float valueInMM)
    {
        m_moveReference = MoveReference.Absolute;
        X(valueInMM);
    }
    public void AY(float valueInMM)
    {
        m_moveReference = MoveReference.Absolute;
        Y(valueInMM);
    }
    public void AZ(float valueInMM)
    {
        m_moveReference = MoveReference.Absolute;
        Z(valueInMM);
    }


    private void MoveInDirection(GVector3 direction)
    {
        if(m_moveReference==MoveReference.Relative)
            SendCommandIfConnection(GCode3018Pro.AsRelative());
        else SendCommandIfConnection(GCode3018Pro.AsAbsolute());

        if (!m_isMotorTheoriclyOn)
            SendCommandIfConnection(GCode3018Pro.FastMove(direction));
        else
            SendCommandIfConnection(GCode3018Pro.ControlledMove(direction, m_speedRateInMm));
    }

}


public abstract class GCodeAction
{
    public abstract string GetCommande();
}
public abstract class GCodeActionCollection
{

    public abstract  string[] GetCommandes();
}


//public class MotorOnOff : GCodeAction
//{

//    public bool m_setMotorOn;

//    public MotorOnOff(bool setMotorOn)
//    {
//        this.m_setMotorOn = setMotorOn;
//    }

//    public override string GetCommande()
//    {
//        throw new System.NotImplementedException();
//    }
//}
//public class FastMove : GCodeActionCollection
//{
//    public Vector3 m_moveValueInMM= new Vector3();
//    public MoveReference m_moveReference;

//    public FastMove()
//    {
//    }
//    public FastMove(Vector3 moveInMM, MoveReference reference)
//    {
//        m_moveReference = reference;
//        m_moveValueInMM = moveInMM;
//    }

//    public override string [] GetCommandes()
//    {
//        return new string[] {

//        };
//    }
//}


public enum MoveType { Fast, Full}
public enum MoveReference { Absolute, Relative }

public class GCode3018Pro {

    public static string AsMM() { return "G21"; }
    public static string AsFeedRateUnityPerMinute() { return "G94"; }

    public static string AsPlaneXY() { return "G17"; }
    public static string AsPlaneZX() { return "G18"; }
    public static string AsPlaneYZ() { return "G19"; }


    public static string AsRelative() { return "G91"; }
    public static string AsAbsolute() { return "G90"; }




    public static string FastMove(GVector3 value)
    {
        return string.Format("G0 X{0} Y{1} Z{2}",
             value.GetWidth(), value.GetDepth(), value.GetHeight()).Replace(',','.');
    }
    public static string ControlledMove(GVector3 value, float feedrate, float extraction)
    {
        return string.Format("G1 X{0} Y{1} Z{2} F{3} E{4}",
            value.GetWidth(), value.GetDepth(), value.GetHeight(), feedrate, extraction).Replace(',', '.');
    }
    public static string ControlledMove(GVector3 value, float feedrate)
    {
        return string.Format("G1 X{0} Y{1} Z{2} F{3} ",
            value.GetWidth(), value.GetDepth(), value.GetHeight(), feedrate).Replace(',', '.');
    }
    public static string ControlledMove(GVector3 value)
    {
        return string.Format("G1 X{0} Y{1} Z{2}",
            value.GetWidth(), value.GetDepth(), value.GetHeight()).Replace(',', '.');
    }

    public static string SetHomeWithCurrent() { return "G28.1 X0 Y0 Z0"; }
    public static string SetHome(GVector3 value)
    {
        return string.Format("G28.1 X{0} Y{1} Z{2}", value.GetWidth(), value.GetDepth(), value.GetHeight()).Replace(',', '.');
    }
    public static string SaveWorkspace()
    {
        return "G10 P0 L20 X0 Y0 Z0";
    }
    public static string SaveWorkspace_Z()
    {

        return "G10 P0 L20 Z0";
    }
    public static string SaveWorkspace_XY()
    {

        return "G10 P0 L20 X0 Y0";
    }
    public static string SaveWorkspace_X()
    {
        return "G10 P0 L20 X0";
    }
    public static string SaveWorkspace_Y()
    {
        return "G10 P0 L20 Y0";
    }
    public static string UseTrack(int index)
    {
        switch (index)
        {
            case 0: return ("G53"); 
            case 1: return ("G54"); 
            case 2: return ("G55"); 
            case 3: return ("G56"); 
            case 4: return ("G57"); 
            case 5: return ("G58"); 
            case 6: return ("G59"); 
            default: return "";
                
        }
    }
    
    public static string Stop(GStopType type)
    {
        if (type == GStopType.M0_Regardless) return "M0";
        if (type == GStopType.M1_Sleep) return "M1";
        if (type == GStopType.M2) return "M2";
        if (type == GStopType.M30_ProgramEnd) return "M30";
        if (type == GStopType.M60_Temporarily) return "M60";
        return "M0";
    }




    public static string GoHome() { return "G28"; }

    public static string StartMotorClockwise() { return ("M3"); }
    public static string StartMotorCounterClockwise() { return ("M4"); }
    public static string StopMotor() { return ("M5"); }

    public static string RotationSpeed(float rotationByMinute) { return "S" + rotationByMinute; }
    public static string RotationZero(float rotationByMinute) { return "S0" ; }
    public static string RotationMin(float rotationByMinute) { return "S1" ; }
    public static string RotationMax(float rotationByMinute) { return "S1000"; }


    public static string StartCoolant() { return "M8"; }
    public static string StopCoolant() { return "M9"; }
    public static string StartVacuum() { return "M10"; }
    public static string StopVacuum() { return "M11"; }




    public class Group {

        public static string[] ClassicStart()
        {
            return new string[] {
                AsAbsolute(),
                AsFeedRateUnityPerMinute(),
                AsPlaneXY(),
                StartCoolant(),
                AsMM(),
                GoHome()
            };
        }
        public static string[] ClassicStop()
        {
            return new string[] {

                StopCoolant(),
                StopMotor(),
                Stop( GStopType.M30_ProgramEnd)
            };
        }
        public static string [] FastMove(MoveReference reference, GVector3 value)
        {
            return new string[] {
                reference==MoveReference.Absolute? AsAbsolute(): AsRelative(),
                GCode3018Pro.FastMove(value)
            };
            
        }
        public static string[] ControlledMove(MoveReference reference, GVector3 value, float feedrate, float extraction)
        {
            return new string[] {
                reference==MoveReference.Absolute? AsAbsolute(): AsRelative(),
                GCode3018Pro.ControlledMove(value, feedrate, extraction)
            };
        }
        public static string[] ControlledMove(MoveReference reference, GVector3 value, float feedrate)
        {
            return new string[] {
                reference==MoveReference.Absolute? AsAbsolute(): AsRelative(),
                GCode3018Pro.ControlledMove(value, feedrate)
            };
        }
        public static string [] ControlledMove(MoveReference reference, GVector3 value)
        {
            return new string[] {
                reference==MoveReference.Absolute? AsAbsolute(): AsRelative(),
                GCode3018Pro.ControlledMove(value )
            };
        }

    }
}

public class GVector3{
    private float m_width;
    private float m_depth;
    private float m_height;

    public GVector3(float width, float depth, float height) {
        m_width = width;
        m_depth = depth;
        m_height = height;
    }

    public float GetWidth() { return m_width; }
    public float GetDepth() { return m_depth; }
    public float GetHeight() { return m_height;}

    public static GVector3 Width(float value) { return new GVector3(value,0,0); }
    public static GVector3 Depth(float value) { return new GVector3(0, value, 0); }
    public static GVector3 Height(float value) { return new GVector3(0, 0, value); }

    internal static GVector3 Zero()
    {
        return new GVector3(0, 0, 0);
    }
}

public enum GStopType{ M0_Regardless , M1_Sleep , M2, M30_ProgramEnd, M60_Temporarily }