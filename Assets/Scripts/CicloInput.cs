using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CicloInput : MonoBehaviour
{
    private Thread IOThread = new Thread(DataThread);
    private static SerialPort sp;
    private static string incomingMsg = "";
    private static string jsonMsg = "";
    private static string outcomingMsg = "";

    private static void DataThread()
    {
        sp = new SerialPort("COM4", 9600);
        sp.Open();

        while (true)
        {
            // if (outcomingMsg != "")
            // {
            //     // sp.
            //     sp.Write(outcomingMsg);
            //     outcomingMsg = "";
            // }

            incomingMsg = sp.ReadExisting();
            jsonMsg = GetValidValue(incomingMsg);
            Thread.Sleep(200);
        }
    }

    public static string GetValidValue(string text)
    {
        // Divide a string em linhas usando os caracteres de nova linha
        string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
        {
            if (line.StartsWith('{') && line.EndsWith('}'))
            {
                return line;
            }
        }

        return null;
    }
    
    private void OnDestroy()
    {
        IOThread.Abort();
        sp.Close();
    }

    void Start()
    {
        IOThread.Start();
        wheelCircumference = Mathf.PI * wheelDiameter; 
    }
    public class CicleInfo
    {
        public int numCycles, cycleTime, spo2, heartRate;
    }
    
    public float wheelDiameter = 0.7f; // Diâmetro da roda em metros
    private float wheelCircumference;
    private float previousCycles = 0;
    private float currentCycles = 0;
    private float deltaTime = 0.1f;
    
    private bool isFirstTime = true;
    
    public CicleInfo info = new CicleInfo();

    public float speed = 0;
    public int maxCicleValue = 0;
    public GameObject player;

    public Text numCyclesText, cycleTimeText, spo2Text, heartRateText;
    
    void Update()
    {
        if (incomingMsg != "")
        {
            info = JsonUtility.FromJson<CicleInfo>(jsonMsg);
            Debug.Log(jsonMsg);

            numCyclesText.text = "Num Cycles " + info.numCycles;
            cycleTimeText.text = "Cycle Time " + info.cycleTime;
            spo2Text.text = "SPO2 " + info.spo2;
            heartRateText.text = "Heart Rate " + info.heartRate;

            currentCycles = info.numCycles;
            if (currentCycles != 0)
            {
                isFirstTime = false;
            }
            
            speed = CalculateSpeed(previousCycles, currentCycles, deltaTime) / 10;

            // Debug.Log("Velocidade da bicicleta: " + speed + " m/s");

            // Atualizar previousCycles para a próxima medição
            previousCycles = currentCycles;
            
            // speed = (((info.numCycles / 999) - 1) * -1) / 10;
            // Debug.Log(incomingMsg);

            // if(info.numCycles)
            // maxCicleValue =
            //
            // speed = 
        }

        if (!isFirstTime)
        {
            player.transform.position = new Vector3(player.transform.position.x, 0, player.transform.position.z + speed);
        }
        


        // if (Input.GetKeyDown(KeyCode.Alpha1))
        // {
        //     outcomingMsg = "0";
        // }
        // else if (Input.GetKeyDown(KeyCode.Alpha2))
        // {
        //     outcomingMsg = "1";
        // }
    }
    
    float CalculateSpeed(float previousCycles, float currentCycles, float deltaTime)
    {
        // Calcular a diferença de ciclos
        float cyclesDifference = currentCycles - previousCycles;

        // Calcular a distância percorrida
        float distance = cyclesDifference * wheelCircumference;

        // Calcular a velocidade (distância / tempo)
        float speed = distance / deltaTime;

        return speed;
    }
}
