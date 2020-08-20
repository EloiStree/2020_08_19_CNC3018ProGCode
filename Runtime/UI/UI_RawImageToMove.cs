using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_RawImageToMove : MonoBehaviour
{

    public Texture2D m_useImage;
    public Texture2D m_tempImage;
    public RawImage m_rawImage;
    public AspectRatioFitter m_ratio;
    public Color m_emptyColor = Color.white;
    public float m_mmPerPixels = 1;

    [Header("Debug")]
    Color[] m_pixels;
    public int m_width;
    public int m_height;
    public string[] m_commandsToSend;
    void Start()
    {
        m_pixels = m_useImage.GetPixels();
        m_rawImage.texture = m_useImage;
        m_tempImage = new Texture2D(m_useImage.width, m_useImage.height);
        m_ratio.aspectRatio = (float)m_useImage.width / (float)m_useImage.height;
        m_width = m_useImage.width;
        m_height = m_useImage.height;


        for (int i = 0; i < m_pixels.Length; i++)
        {
            if (m_pixels[i] == Color.white || m_pixels[i].a==0) {
                m_pixels[i] = Color.white;
            }
            else 
                m_pixels[i] = Color.black;
        }
        m_tempImage.SetPixels(m_pixels);
        m_tempImage.Apply();
        m_rawImage.texture = m_tempImage;
    }


    public void StabEachPoints() {
        m_commandsToSend = GetCommands();
        StartCoroutine(DirtyStabTest());
    }

    public float m_timePerCommands = 2;
    private IEnumerator DirtyStabTest()
    {
        if (GCodeConnection.HasConnection())
        {
            GCodeConnection connection = GCodeConnection.GetConnection();
            foreach (string cmd in m_commandsToSend)
            {
                connection.SendCommand(cmd);
                yield return new WaitForSeconds(m_timePerCommands);
            }
        }
    }

    public string[] GetCommands() {
        List<string> cmds = new List<string>();
        int centerX = (int)(m_width*m_mmPerPixels / 2f);
        int centerY = (int)(m_height *m_mmPerPixels / 2f);

        cmds.Add(GCode3018Pro.AsAbsolute());
        for (int i = 0; i < m_pixels.Length; i++)
        {
            float x, y;
            GetCoordonateOf(i, out x, out y);
            x -= centerX;
            y -= centerY;
            x *= m_mmPerPixels;
            y *= m_mmPerPixels;
            if (m_pixels[i] != Color.white) {
                cmds.Add("Z0");
                cmds.Add(GCode3018Pro.FastMove(new GVector3(x, y, 0)));
                cmds.Add("Z-1");
                cmds.Add("Z0");
            }
        }
        return cmds.ToArray();
    }

    public void GetCoordonateOf(int index, out float x, out float y) {
        y = (int)((float)index / (float)m_width);
        x = index % m_width;
    }
}
