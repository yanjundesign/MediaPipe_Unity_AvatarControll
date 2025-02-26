using UnityEngine;
using System.Collections.Generic;

public class AvatarController : MonoBehaviour
{
    [Header("Mixamo Bone References")]
    [SerializeField] private Transform mixamoHips;
    [SerializeField] private Transform mixamoSpine;
    [SerializeField] private Transform mixamoHead;
    [SerializeField] private Transform mixamoLeftShoulder;
    [SerializeField] private Transform mixamoRightShoulder;
    [SerializeField] private Transform mixamoLeftArm;
    [SerializeField] private Transform mixamoRightArm;
    [SerializeField] private Transform mixamoLeftForeArm;
    [SerializeField] private Transform mixamoRightForeArm;
    [SerializeField] private Transform mixamoLeftHand;
    [SerializeField] private Transform mixamoRightHand;
    
    [Header("IK Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool useIK = true;
    [Range(0, 1)]
    [SerializeField] private float headIKWeight = 1.0f;
    [Range(0, 1)]
    [SerializeField] private float leftHandIKWeight = 1.0f;
    [Range(0, 1)]
    [SerializeField] private float rightHandIKWeight = 1.0f;
    
    [Header("Movement Settings")]
    [SerializeField] private float poseSmoothness = 8f; // Increased for smoother transitions
    [SerializeField] private bool mirrorX = true;
    [SerializeField] private float scaleFactor = 0.5f; // Adjusted for better scale
    [SerializeField] private Vector3 rootOffset = new Vector3(0, 1, 0);
    [Range(0.1f, 1.0f)]
    [SerializeField] private float filterFactor = 0.3f; // New parameter for position filtering
    
    [Header("Debug Settings")]
    [SerializeField] private bool debugVisualization = true;
    [SerializeField] private float debugSphereSize = 0.1f;
    
    // Debug visualization objects
    private GameObject headDebugSphere;
    private GameObject leftHandDebugSphere;
    private GameObject rightHandDebugSphere;
    
    // IK Target positions
    private Vector3 headPos;
    private Vector3 leftShoulderPos;
    private Vector3 rightShoulderPos;
    private Vector3 leftElbowPos;
    private Vector3 rightElbowPos;
    private Vector3 leftWristPos;
    private Vector3 rightWristPos;
    
    // For filtering positions (new)
    private Vector3 lastHeadPos;
    private Vector3 lastLeftShoulderPos;
    private Vector3 lastRightShoulderPos;
    private Vector3 lastLeftElbowPos;
    private Vector3 lastRightElbowPos;
    private Vector3 lastLeftWristPos;
    private Vector3 lastRightWristPos;
    
    // IK target GameObjects
    private Transform headTarget;
    private Transform leftHandTarget;
    private Transform rightHandTarget;
    
    // Original rotations
    private Dictionary<Transform, Quaternion> initialRotations = new Dictionary<Transform, Quaternion>();
    
    void Start()
    {
        // Auto-find references if not set
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // Find bone references if not set
        FindMixamoBones();
        
        // Store original rotations
        StoreInitialRotations();
        
        // Create IK targets
        CreateIKTargets();
        
        // Create debug visualization if enabled
        if (debugVisualization)
        {
            CreateDebugVisualizations();
        }
        
        Debug.Log("AvatarController initialized");
    }
    
    private void FindMixamoBones()
    {
        // Find the hips if not set
        if (mixamoHips == null)
        {
            Transform hips = transform.Find("mixamorig:Hips");
            if (hips != null)
            {
                mixamoHips = hips;
                
                // Find spine
                Transform spine = hips.Find("mixamorig:Spine");
                if (spine != null)
                {
                    mixamoSpine = spine;
                    
                    // Follow the spine chain
                    Transform spine1 = spine.Find("mixamorig:Spine1");
                    if (spine1 != null)
                    {
                        Transform spine2 = spine1.Find("mixamorig:Spine2");
                        if (spine2 != null)
                        {
                            // Find neck and head
                            Transform neck = spine2.Find("mixamorig:Neck");
                            if (neck != null)
                            {
                                mixamoHead = neck.Find("mixamorig:Head");
                            }
                            
                            // Find shoulders and arms
                            mixamoLeftShoulder = spine2.Find("mixamorig:LeftShoulder");
                            if (mixamoLeftShoulder != null)
                            {
                                mixamoLeftArm = mixamoLeftShoulder.Find("mixamorig:LeftArm");
                                if (mixamoLeftArm != null)
                                {
                                    mixamoLeftForeArm = mixamoLeftArm.Find("mixamorig:LeftForeArm");
                                    if (mixamoLeftForeArm != null)
                                    {
                                        mixamoLeftHand = mixamoLeftForeArm.Find("mixamorig:LeftHand");
                                    }
                                }
                            }
                            
                            mixamoRightShoulder = spine2.Find("mixamorig:RightShoulder");
                            if (mixamoRightShoulder != null)
                            {
                                mixamoRightArm = mixamoRightShoulder.Find("mixamorig:RightArm");
                                if (mixamoRightArm != null)
                                {
                                    mixamoRightForeArm = mixamoRightArm.Find("mixamorig:RightForeArm");
                                    if (mixamoRightForeArm != null)
                                    {
                                        mixamoRightHand = mixamoRightForeArm.Find("mixamorig:RightHand");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Log warnings for missing bones
        if (mixamoHips == null) Debug.LogWarning("Could not find mixamorig:Hips");
        if (mixamoHead == null) Debug.LogWarning("Could not find mixamorig:Head");
        if (mixamoLeftArm == null) Debug.LogWarning("Could not find mixamorig:LeftArm");
        if (mixamoRightArm == null) Debug.LogWarning("Could not find mixamorig:RightArm");
    }
    
    private void StoreInitialRotations()
    {
        // Store initial rotations for all bones
        if (mixamoHips != null) initialRotations[mixamoHips] = mixamoHips.localRotation;
        if (mixamoSpine != null) initialRotations[mixamoSpine] = mixamoSpine.localRotation;
        if (mixamoHead != null) initialRotations[mixamoHead] = mixamoHead.localRotation;
        if (mixamoLeftShoulder != null) initialRotations[mixamoLeftShoulder] = mixamoLeftShoulder.localRotation;
        if (mixamoRightShoulder != null) initialRotations[mixamoRightShoulder] = mixamoRightShoulder.localRotation;
        if (mixamoLeftArm != null) initialRotations[mixamoLeftArm] = mixamoLeftArm.localRotation;
        if (mixamoRightArm != null) initialRotations[mixamoRightArm] = mixamoRightArm.localRotation;
        if (mixamoLeftForeArm != null) initialRotations[mixamoLeftForeArm] = mixamoLeftForeArm.localRotation;
        if (mixamoRightForeArm != null) initialRotations[mixamoRightForeArm] = mixamoRightForeArm.localRotation;
        if (mixamoLeftHand != null) initialRotations[mixamoLeftHand] = mixamoLeftHand.localRotation;
        if (mixamoRightHand != null) initialRotations[mixamoRightHand] = mixamoRightHand.localRotation;
    }
    
    private void CreateIKTargets()
    {
        if (useIK)
        {
            // Create head target
            GameObject headObj = new GameObject("HeadIKTarget");
            headTarget = headObj.transform;
            headTarget.SetParent(transform);
            headTarget.localPosition = new Vector3(0, 1.7f, 0); // Position at approximate head height
            
            // Create hand targets
            GameObject leftHandObj = new GameObject("LeftHandIKTarget");
            leftHandTarget = leftHandObj.transform;
            leftHandTarget.SetParent(transform);
            leftHandTarget.localPosition = new Vector3(-0.5f, 1.0f, 0); // Position at approximate left hand position
            
            GameObject rightHandObj = new GameObject("RightHandIKTarget");
            rightHandTarget = rightHandObj.transform;
            rightHandTarget.SetParent(transform);
            rightHandTarget.localPosition = new Vector3(0.5f, 1.0f, 0); // Position at approximate right hand position
            
            Debug.Log("IK targets created");
        }
    }
    
    private void CreateDebugVisualizations()
    {
        // Create head debug sphere
        headDebugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headDebugSphere.name = "HeadDebugSphere";
        headDebugSphere.transform.localScale = Vector3.one * debugSphereSize;
        Renderer headRenderer = headDebugSphere.GetComponent<Renderer>();
        headRenderer.material.color = Color.red;
        // Remove collider to avoid physics interactions
        Destroy(headDebugSphere.GetComponent<Collider>());
        
        // Create left hand debug sphere
        leftHandDebugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftHandDebugSphere.name = "LeftHandDebugSphere";
        leftHandDebugSphere.transform.localScale = Vector3.one * debugSphereSize;
        Renderer leftHandRenderer = leftHandDebugSphere.GetComponent<Renderer>();
        leftHandRenderer.material.color = Color.green;
        Destroy(leftHandDebugSphere.GetComponent<Collider>());
        
        // Create right hand debug sphere
        rightHandDebugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightHandDebugSphere.name = "RightHandDebugSphere";
        rightHandDebugSphere.transform.localScale = Vector3.one * debugSphereSize;
        Renderer rightHandRenderer = rightHandDebugSphere.GetComponent<Renderer>();
        rightHandRenderer.material.color = Color.blue;
        Destroy(rightHandDebugSphere.GetComponent<Collider>());
        
        Debug.Log("Debug visualization spheres created");
    }
    
    // Method called by the UDPReceiver to update the pose
    public void UpdatePose(Vector3 head, Vector3 leftShoulder, Vector3 rightShoulder, 
                          Vector3 leftElbow, Vector3 rightElbow, Vector3 leftWrist, Vector3 rightWrist)
    {
        // Convert the raw positions to Unity space
        Vector3 rawHeadPos = ConvertToUnitySpace(head);
        Vector3 rawLeftShoulderPos = ConvertToUnitySpace(leftShoulder);
        Vector3 rawRightShoulderPos = ConvertToUnitySpace(rightShoulder);
        Vector3 rawLeftElbowPos = ConvertToUnitySpace(leftElbow);
        Vector3 rawRightElbowPos = ConvertToUnitySpace(rightElbow);
        Vector3 rawLeftWristPos = ConvertToUnitySpace(leftWrist);
        Vector3 rawRightWristPos = ConvertToUnitySpace(rightWrist);
        
        // Debug logs to verify the positions
        Debug.Log($"Head: {head} -> {rawHeadPos}");
        Debug.Log($"Left Shoulder: {leftShoulder} -> {rawLeftShoulderPos}");
        Debug.Log($"Right Shoulder: {rightShoulder} -> {rawRightShoulderPos}");
        Debug.Log($"Left Elbow: {leftElbow} -> {rawLeftElbowPos}");
        Debug.Log($"Right Elbow: {rightElbow} -> {rawRightElbowPos}");
        Debug.Log($"Left Wrist: {leftWrist} -> {rawLeftWristPos}");
        Debug.Log($"Right Wrist: {rightWrist} -> {rawRightWristPos}");

        
        // Apply filtering to smooth out the movements
        // If this is the first frame, just use the raw positions
        if (lastHeadPos == Vector3.zero)
        {
            lastHeadPos = rawHeadPos;
            lastLeftShoulderPos = rawLeftShoulderPos;
            lastRightShoulderPos = rawRightShoulderPos;
            lastLeftElbowPos = rawLeftElbowPos;
            lastRightElbowPos = rawRightElbowPos;
            lastLeftWristPos = rawLeftWristPos;
            lastRightWristPos = rawRightWristPos;
        }
        
        // Blend between the last position and the new position for smoother movement
        headPos = Vector3.Lerp(lastHeadPos, rawHeadPos, filterFactor);
        leftShoulderPos = Vector3.Lerp(lastLeftShoulderPos, rawLeftShoulderPos, filterFactor);
        rightShoulderPos = Vector3.Lerp(lastRightShoulderPos, rawRightShoulderPos, filterFactor);
        leftElbowPos = Vector3.Lerp(lastLeftElbowPos, rawLeftElbowPos, filterFactor);
        rightElbowPos = Vector3.Lerp(lastRightElbowPos, rawRightElbowPos, filterFactor);
        leftWristPos = Vector3.Lerp(lastLeftWristPos, rawLeftWristPos, filterFactor);
        rightWristPos = Vector3.Lerp(lastRightWristPos, rawRightWristPos, filterFactor);
        
        // Store the filtered positions for the next frame
        lastHeadPos = headPos;
        lastLeftShoulderPos = leftShoulderPos;
        lastRightShoulderPos = rightShoulderPos;
        lastLeftElbowPos = leftElbowPos;
        lastRightElbowPos = rightElbowPos;
        lastLeftWristPos = leftWristPos;
        lastRightWristPos = rightWristPos;
    }
    
    private Vector3 ConvertToUnitySpace(Vector3 position)
    {
       // Mirror X axis if needed (based on camera setup)
    float x = mirrorX ? -position.x : position.x;
    
    // Invert Y axis (MediaPipe Y increases downward, Unity Y increases upward)
    float y = -position.y;
    
    // Z axis handling - sometimes needs to be inverted based on camera orientation
    float z = -position.z;
    
    
    // Apply scale and offset
    Vector3 convertedPosition = new Vector3(x, y, z) * scaleFactor + rootOffset;
    
    // Debug log to verify the conversion
    Debug.Log($"Original: {position}, Converted: {convertedPosition}");
    
    return convertedPosition;
    }
    
    void Update()
    {
        if (useIK)
            UpdateIKTargets();
        else
            UpdateFKPose();
            
        // Update debug visualization
        if (debugVisualization)
        {
            if (headDebugSphere != null)
                headDebugSphere.transform.position = transform.position + headPos;
                
            if (leftHandDebugSphere != null)
                leftHandDebugSphere.transform.position = transform.position + leftWristPos;
                
            if (rightHandDebugSphere != null)
                rightHandDebugSphere.transform.position = transform.position + rightWristPos;
        }
    }
    
    private void UpdateIKTargets()
    {
        // Update IK target positions with additional smoothing
        if (headTarget != null && headPos != Vector3.zero)
        {
            Vector3 targetPos = transform.position + headPos;
            headTarget.position = Vector3.Lerp(headTarget.position, targetPos, Time.deltaTime * poseSmoothness);
        }
        
        if (leftHandTarget != null && leftWristPos != Vector3.zero)
        {
            Vector3 targetPos = transform.position + leftWristPos;
            leftHandTarget.position = Vector3.Lerp(leftHandTarget.position, targetPos, Time.deltaTime * poseSmoothness);
        }
        
        if (rightHandTarget != null && rightWristPos != Vector3.zero)
        {
            Vector3 targetPos = transform.position + rightWristPos;
            rightHandTarget.position = Vector3.Lerp(rightHandTarget.position, targetPos, Time.deltaTime * poseSmoothness);
        }
    }
    
    private void UpdateFKPose()
    {
        // Update bone rotations using forward kinematics
        
        // Left arm chain
        if (mixamoLeftArm != null && leftShoulderPos != Vector3.zero && leftElbowPos != Vector3.zero)
        {
            Vector3 upperArmDir = (leftElbowPos - leftShoulderPos).normalized;
            if (upperArmDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(upperArmDir);
                mixamoLeftArm.rotation = Quaternion.Slerp(mixamoLeftArm.rotation, targetRot, Time.deltaTime * poseSmoothness);
            }
        }
        
        if (mixamoLeftForeArm != null && leftElbowPos != Vector3.zero && leftWristPos != Vector3.zero)
        {
            Vector3 forearmDir = (leftWristPos - leftElbowPos).normalized;
            if (forearmDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(forearmDir);
                mixamoLeftForeArm.rotation = Quaternion.Slerp(mixamoLeftForeArm.rotation, targetRot, Time.deltaTime * poseSmoothness);
            }
        }
        
        // Right arm chain
        if (mixamoRightArm != null && rightShoulderPos != Vector3.zero && rightElbowPos != Vector3.zero)
        {
            Vector3 upperArmDir = (rightElbowPos - rightShoulderPos).normalized;
            if (upperArmDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(upperArmDir);
                mixamoRightArm.rotation = Quaternion.Slerp(mixamoRightArm.rotation, targetRot, Time.deltaTime * poseSmoothness);
            }
        }
        
        if (mixamoRightForeArm != null && rightElbowPos != Vector3.zero && rightWristPos != Vector3.zero)
        {
            Vector3 forearmDir = (rightWristPos - rightElbowPos).normalized;
            if (forearmDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(forearmDir);
                mixamoRightForeArm.rotation = Quaternion.Slerp(mixamoRightForeArm.rotation, targetRot, Time.deltaTime * poseSmoothness);
            }
        }
        
        // Update head to look in the right direction
        if (mixamoHead != null && headPos != Vector3.zero)
        {
            // Use head position relative to spine to determine head orientation
            if (mixamoSpine != null)
            {
                Vector3 lookDir = headPos - mixamoSpine.position;
                if (lookDir != Vector3.zero)
                {
                    // Create a rotation that looks in the direction of the head
                    Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                    mixamoHead.rotation = Quaternion.Slerp(mixamoHead.rotation, targetRot, Time.deltaTime * poseSmoothness);
                }
            }
        }
    }
    
    void OnAnimatorIK(int layerIndex)
    {
        if (!useIK || animator == null) return;
        
        // Set head look target
        if (headTarget != null && headPos != Vector3.zero)
        {
            animator.SetLookAtWeight(headIKWeight);
            animator.SetLookAtPosition(headTarget.position);
        }
        
        // Set hand IK
        if (leftHandTarget != null && leftWristPos != Vector3.zero)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandIKWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
        }
        
        if (rightHandTarget != null && rightWristPos != Vector3.zero)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
        }
    }
    
    void OnDestroy()
    {
        // Clean up debug visualization
        if (debugVisualization)
        {
            if (headDebugSphere != null)
                Destroy(headDebugSphere);
                
            if (leftHandDebugSphere != null)
                Destroy(leftHandDebugSphere);
                
            if (rightHandDebugSphere != null)
                Destroy(rightHandDebugSphere);
        }
        
        // Clean up IK targets
        if (useIK)
        {
            if (headTarget != null)
                Destroy(headTarget.gameObject);
                
            if (leftHandTarget != null)
                Destroy(leftHandTarget.gameObject);
                
            if (rightHandTarget != null)
                Destroy(rightHandTarget.gameObject);
        }
    }
}