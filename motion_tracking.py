import cv2 # type: ignore
import mediapipe as mp # type: ignore
import socket
import json

# Initialize UDP
UDP_IP = "127.0.0.1"
UDP_PORT = 5052

# Create UDP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Initialize MediaPipe Pose with better settings
mp_pose = mp.solutions.pose
pose = mp_pose.Pose(
    min_detection_confidence=0.7,    # Increased from 0.5
    min_tracking_confidence=0.5,
    model_complexity=1,              # Added for better accuracy
    static_image_mode=False         # Added for better performance
)
mp_drawing = mp.solutions.drawing_utils

# Initialize webcam with error handling
print("Initializing webcam...")
cap = cv2.VideoCapture(0)
if not cap.isOpened():
    print("Error: Could not open camera")
    exit()

# Set camera properties with verification
def set_camera_property(cap, prop, value):
    cap.set(prop, value)
    actual = cap.get(prop)
    print(f"Requested {prop}: {value}, Actual: {actual}")

set_camera_property(cap, cv2.CAP_PROP_FRAME_WIDTH, 1280)
set_camera_property(cap, cv2.CAP_PROP_FRAME_HEIGHT, 720)
set_camera_property(cap, cv2.CAP_PROP_FPS, 30)

# Create window
cv2.namedWindow('MediaPipe Pose Detection', cv2.WINDOW_NORMAL)
cv2.resizeWindow('MediaPipe Pose Detection', 1280, 720)

def put_coordinates_text(frame, landmark, idx, label=""):
    h, w, _ = frame.shape
    px = int(landmark.x * w)
    py = int(landmark.y * h)
    
    text = f"{label}#{idx} x:{landmark.x:.2f} y:{landmark.y:.2f} z:{landmark.z:.2f}"
    
    (text_w, text_h), _ = cv2.getTextSize(text, cv2.FONT_HERSHEY_SIMPLEX, 0.5, 1)
    cv2.rectangle(frame, (px, py - text_h - 4), (px + text_w, py), (0, 0, 0), -1)
    cv2.putText(frame, text, (px, py - 5), 
                cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)

try:
    while True:
        ret, frame = cap.read()
        if not ret:
            print("Error: Could not read frame")
            continue

        # Process image
        image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = pose.process(image_rgb)

        if results.pose_landmarks:
            # Draw landmarks
            mp_drawing.draw_landmarks(
                frame,
                results.pose_landmarks,
                mp_pose.POSE_CONNECTIONS,
                mp_drawing.DrawingSpec(color=(0, 255, 0), thickness=5, circle_radius=5),
                mp_drawing.DrawingSpec(color=(255, 0, 0), thickness=5)
            )
            
            # Prepare data
            KEY_LANDMARKS = {
                0: "Head",
                11: "L.Shoulder", 12: "R.Shoulder",
                13: "L.Elbow", 14: "R.Elbow",
                15: "L.Wrist", 16: "R.Wrist"
            }
            
            # Create JSON structure
            landmark_data = {
                "pose": [
                    {
                        "index": idx,
                        "name": KEY_LANDMARKS.get(idx, ""),
                        "x": landmark.x,
                        "y": landmark.y,
                        "z": landmark.z
                    }
                    for idx, landmark in enumerate(results.pose_landmarks.landmark)
                    if idx in KEY_LANDMARKS
                ]
            }
            
            # Display coordinates for key landmarks
            for point in landmark_data["pose"]:
                idx = point["index"]
                landmark = results.pose_landmarks.landmark[idx]
                put_coordinates_text(frame, landmark, idx, point["name"])
                print(f"{point['name']} ({idx}): x={point['x']:.3f}, y={point['y']:.3f}, z={point['z']:.3f}")
            
            # Send data
            try:
                data_json = json.dumps(landmark_data)
                sock.sendto(data_json.encode('utf-8'), (UDP_IP, UDP_PORT))
            except Exception as e:
                print(f"Error sending data: {e}")

        # Add legend
        cv2.putText(frame, "Landmark Guide:", (10, 30), 
                    cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)
        cv2.putText(frame, "#0: Head/Nose", (10, 60), 
                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)
        cv2.putText(frame, "#11,12: Shoulders", (10, 80), 
                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)
        cv2.putText(frame, "#13,14: Elbows", (10, 100), 
                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)
        cv2.putText(frame, "#15,16: Wrists", (10, 120), 
                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)

        cv2.imshow('MediaPipe Pose Detection', frame)
        
        if cv2.waitKey(1) & 0xFF == ord('q'):
            print("Quitting...")
            break

finally:
    print("Cleaning up...")
    cap.release()
    cv2.destroyAllWindows()
    sock.close()