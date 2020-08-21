using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendToGRBLMono : MonoBehaviour
{
   

    public void SendPauseRequest()
    {
        GRBLConnection.TryToSendCommand(GCode3018Pro.PauseDeviceJobs());
    }
    public void SendResumeRequest()
    {
        GRBLConnection.TryToSendCommand(GCode3018Pro.ResumeDeviceJobs());
    }
    public void SendFullStopRequest() {
        //Not sure of what is the best "stop solution"
//        GCodeConnection.TryToSendCommand(""+GCode3018Pro.GRBL.JogCancel());
    }


    #region Did not work when teste but should
    public void ToggleSpindle() => GRBLConnection.TryToSendCommand("" + GCode3018Pro.GRBL.ToggleSpindle());
    public void ToggleFloodCooler() => GRBLConnection.TryToSendCommand("" + GCode3018Pro.GRBL.ToggleFloodCoolant());
    public void ToggleMistCooler() => GRBLConnection.TryToSendCommand("" + GCode3018Pro.GRBL.ToggleMistCoolant());
    #endregion
}
