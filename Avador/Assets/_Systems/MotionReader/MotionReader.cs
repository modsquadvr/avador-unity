using System;
using System.IO.Ports;
using UnityEngine;


/// <summary>
/// Requires arudino_motionsensor script to be flashed to the arduino.
/// Currently the script has motion sensor output going to pin 7, 
/// and pin 7 gets written to COM3 every 500 ms.
/// </summary>
public class MotionReader : MonoBehaviour
{
    //Read COM3, 9600baud
    static readonly SerialPort serialPort = new SerialPort("COM3", 9600);

    void Start()
     => serialPort.Open();

    void Update()
     => Read();

    void OnDestroy()
     => serialPort.Close();

    private void Read()
    {
        try
        {
            int data = serialPort.ReadByte();

            if (data == '1')
                Debug.Log("Motion Detected");

            else if (data == '0')
                Debug.Log("No Motion");

        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

    }

}
