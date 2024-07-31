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
    private int _initialCycles = 0;
    public Rigidbody playerRigidBody;
    
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
    private float speed = 0;

    // --
    
    // Variáveis para rastrear a rotação das rodas
    private float previousRotation;
    private float currentRotation;

    // Variáveis para controle de velocidade
    public float wheelCircumference = 2.1f; // Circunferência da roda em metros
    public float maxSpeed = 20f;
    
    // --
    
    public class CicleInfo
    {
        public int spo2 = 0, 
                   numCycles = 0,
                   cycleTime = 0,
                   heartRate = 0;
    }

    private void Start()
    {
        IOThread = new(DataThread);
        IOThread.Start();
       
        // --
        
        _wheelCircumference = Mathf.PI * wheelDiameter; 
        
        // --
        
        previousRotation = 0f;
        currentRotation = 0f;
    }

    private int _previousCycle = 0;
    private float _forceByStep = 2f;
    private float _maxMagnitude = 10f;
    private void Update()
    {
        if (logSerial) Debug.Log(_jsonMsg);
        
        player.transform.Rotate(0, Input.GetAxis("Horizontal") * 50f * Time.deltaTime, 0);
        
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

            if (_isFirstTime)
            {
                _initialCycles = _info.numCycles;
                _isFirstTime = false;
            }

            if (_info.numCycles != previousRotation)
            {
                Debug.Log(">" + _info.numCycles + " - " + previousRotation);
                
                if (playerRigidBody.velocity.magnitude < _maxMagnitude)
                {
                    playerRigidBody.AddForce(player.transform.forward * _forceByStep, ForceMode.Impulse);
                }
                else
                {
                    playerRigidBody.AddForce(player.transform.forward * _maxMagnitude, ForceMode.VelocityChange);
                }
            }
            
            // currentRotation = _info.numCycles - _initialCycles;
            
            // float speed = CalculateSpeed(previousRotation, currentRotation, Time.deltaTime) * 10f;
            // playerRigidBody.AddForce(transform.forward * Mathf.Min(speed, maxSpeed), ForceMode.Impulse);
            // previousRotation = currentRotation;
            // previousRotation = _info.numCycles;

            previousRotation = _info.numCycles;
            // _previousCycle = _info.numCycles;

            //
            // _currentCycles = _info.numCycles;
            //
            // speed = CalculateSpeed(_previousCycles, _currentCycles, _deltaTime) / 10;
            // _previousCycles = _currentCycles;

            #endregion
        }

        playerRigidBody.AddForce(new Vector3(0, 0, speed * 1000f), ForceMode.Impulse);
    }
    
    private void OnDestroy()
    {
        IOThread.Abort();
        _serialPort.Close();
    }
    
    float GetWheelRotation()
    {
        return Time.time * 360f;
    }
    
    /*
     * <summary>
     * Resgate da informação que chega na porta serial.
     * </summary>
     */ 
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

     /**
      * <summary>
      * Processa a string de entrada da porta serial e retorna uma string com um valor JSON válido.
      * </summary>
      * <param name="text">String a ser processada</param>
      *<returns>Uma string com um JSON válido se houver, nulo se não houver.</returns>
      */ 
    public static string GetValidValue(string text)
    {
        string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
            if (line.StartsWith('{') && line.EndsWith('}'))
                return line;
        
        return null;
    }
    
    /**
     * <summary>
     * Calcula a velocidade da bicicleta baseado nos ciclos.
     * </summary>
     * <param name="previousCycles">Quantidade de ciclos anterior.</param>
     * <param name="currentCycles">Quantidade de ciclos atual.</param>
     * <param name="deltaTime">Variação de tempo.</param> 
     */ 
    float CalculateSpeed(float previousCycles, float currentCycles, float deltaTime)
    {
        float rotationDifference = currentRotation - previousRotation;
        float distanceTraveled = (rotationDifference / 360f) * wheelCircumference;
        float speed = distanceTraveled / deltaTime;

        return speed;
    }
}
