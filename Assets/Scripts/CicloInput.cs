using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class CicloInput : MonoBehaviour
{
    [Header("Base Configuration")]
    public String port = "COM4";
    public Boolean logSerial = false;
    public GameObject player;
    
    [Header("Text Configuration")]
    public Text numCyclesCanvasText;
    public Text cycleTimeCanvasText; 
    public Text spo2CanvasText; 
    public Text heartRateCanvasText;
    
    private Thread IOThread;
    private CicleInfo _info = new();
    private static SerialPort _serialPort;
    private static string _jsonMsg = "";
    private static string _incomingMsg = "";
    
    // --    
    
    private readonly float _deltaTime = 0.1f;
    private float _wheelCircumference;
    private bool _isFirstTime = true;
    private float _previousCycles;
    private float _currentCycles;
    
    [Header("Virtual Bike Configuration")]
    public float wheelDiameter = 0.7f;
    public float speed = 0;

    // --
    
    public class CicleInfo
    {
        public int spo2 = 0, 
                   numCycles = 0,
                   cycleTime = 0,
                   heartRate = 0;
    }
    
    private void OnDestroy()
    {
        IOThread.Abort();
        _serialPort.Close();
    }

    private void Start()
    {
        IOThread = new(DataThread);
        IOThread.Start();
        
        // --
        
        _wheelCircumference = Mathf.PI * wheelDiameter; 
    }
    
    private void Update()
    {
        if (logSerial) Debug.Log(_jsonMsg);
        
        if (_incomingMsg != "")
        {
            // Converte JSON para objeto
            _info = JsonUtility.FromJson<CicleInfo>(_jsonMsg);

            #region Setup Visual Info
            
                spo2CanvasText.text = "SPO2 " + _info.spo2;
                numCyclesCanvasText.text = "Num Cycles " + _info.numCycles;
                cycleTimeCanvasText.text = "Cycle Time " + _info.cycleTime;
                heartRateCanvasText.text = "Heart Rate " + _info.heartRate;

            #endregion
            
            #region Calculate Bike Speed

                _currentCycles = _info.numCycles;
                
                if (_currentCycles != 0)
                    _isFirstTime = false;
                
                speed = CalculateSpeed(_previousCycles, _currentCycles, _deltaTime) / 10;
                _previousCycles = _currentCycles;
            
            #endregion
        }

        if (!_isFirstTime)
        {
            player.transform.position = new Vector3(player.transform.position.x, 0, player.transform.position.z + speed);
        }
    }
    
    /// <summary>
    /// Resgate da informação que chega na porta serial.
    /// </summary>
    private void DataThread()
    {
        _serialPort = new SerialPort(port, 9600);
        _serialPort.Open();

        while (true)
        {
            _incomingMsg = _serialPort.ReadExisting();
            _jsonMsg = GetValidValue(_incomingMsg);
            Thread.Sleep(200);
        }
    }

    /// <summary>
    /// Processa a string de entrada da porta serial e retorna uma string com um valor JSON válido.
    /// </summary>
    /// <param name="text">String a ser processada</param>
    /// <returns>Uma string com um JSON válido se houver, nulo se não houver.</returns>
    public static string GetValidValue(string text)
    {
        string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
            if (line.StartsWith('{') && line.EndsWith('}'))
                return line;
        
        return null;
    }
    
    /// <summary>
    /// Calcula a velocidade da bicicleta baseado nos ciclos.
    /// </summary>
    /// <param name="previousCycles">Quantidade de ciclos anterior.</param>
    /// <param name="currentCycles">Quantidade de ciclos atual.</param>
    /// <param name="deltaTime">Variação de tempo.</param>
    /// <returns>Velocidade atual da bicicleta.</returns>
    float CalculateSpeed(float previousCycles, float currentCycles, float deltaTime)
    {
        float cyclesDifference = currentCycles - previousCycles;
        float distance = cyclesDifference * _wheelCircumference;
        return distance / deltaTime;
    }
}
