using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Collections.Generic;

public class UDPReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    private const int PORT = 5052;
    private IPEndPoint endPoint;

    [SerializeField] private AvatarController avatarController;

    void Start()
    {
        try
        {
            udpClient = new UdpClient(PORT);
            endPoint = new IPEndPoint(IPAddress.Any, PORT);
            Debug.Log($"<color=green>UDP Receiver started on port {PORT}</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start UDP client: {e.Message}");
        }
    }

    void Update()
    {
        if (udpClient != null && udpClient.Available > 0)
        {
            try
            {
                byte[] data = udpClient.Receive(ref endPoint);
                string jsonString = Encoding.UTF8.GetString(data);
                PoseData poseData = JsonUtility.FromJson<PoseData>(jsonString);
                
                if (poseData != null && poseData.pose != null)
                {
                    // Create dictionary to store positions
                    Dictionary<string, Vector3> positions = new Dictionary<string, Vector3>();
                    
                    foreach (var landmark in poseData.pose)
                    {
                        positions[landmark.name] = new Vector3(landmark.x, landmark.y, landmark.z);
                    }

                    // Send positions to avatar controller if available
                    if (avatarController != null && positions.ContainsKey("Head") && 
                        positions.ContainsKey("L.Shoulder") && positions.ContainsKey("R.Shoulder") &&
                        positions.ContainsKey("L.Elbow") && positions.ContainsKey("R.Elbow") &&
                        positions.ContainsKey("L.Wrist") && positions.ContainsKey("R.Wrist"))
                    {
                        avatarController.UpdatePose(
                            positions["Head"],
                            positions["L.Shoulder"],
                            positions["R.Shoulder"],
                            positions["L.Elbow"],
                            positions["R.Elbow"],
                            positions["L.Wrist"],
                            positions["R.Wrist"]
                        );
                    }

                    // Log the data with timestamps
                    StringBuilder messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine($"[{System.DateTime.Now.ToString("HH:mm:ss")}] Current Pose Data:");
                    
                    foreach (var landmark in poseData.pose)
                    {
                        string coordsFormat = $"x:{landmark.x:F3}, y:{landmark.y:F3}, z:{landmark.z:F3}";
                        string coloredText = "";
                        
                        switch (landmark.name)
                        {
                            case "Head":
                                coloredText = $"<color=yellow>{landmark.name}: {coordsFormat}</color>";
                                break;
                            case "L.Shoulder":
                            case "R.Shoulder":
                                coloredText = $"<color=blue>{landmark.name}: {coordsFormat}</color>";
                                break;
                            case "L.Elbow":
                            case "R.Elbow":
                                coloredText = $"<color=green>{landmark.name}: {coordsFormat}</color>";
                                break;
                            case "L.Wrist":
                            case "R.Wrist":
                                coloredText = $"<color=magenta>{landmark.name}: {coordsFormat}</color>";
                                break;
                        }
                        messageBuilder.AppendLine(coloredText);
                    }
                    
                    Debug.Log(messageBuilder.ToString());
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error receiving data: {e.Message}");
            }
        }
    }

    void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }

    [System.Serializable]
    public class PoseLandmark
    {
        public int index;
        public string name;
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class PoseData
    {
        public PoseLandmark[] pose;
    }
}