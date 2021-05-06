/* 
*   Detector Cam
*   Copyright (c) 2021 Yusuf Olokoba.
*/

namespace NatSuite.Examples {

    using UnityEngine;
    using UnityEngine.UI;
    using Recorders;
    using Recorders.Inputs;
    using Sharing;

    [RequireComponent(typeof(Button))]
    public class Recorder : MonoBehaviour {

        [Header("Recording")]
        [SerializeField] Vector2Int size = new Vector2Int(480, 640);

        [Header("UI")]
        [SerializeField] Button button;
        [SerializeField] Image image;

        GIFRecorder recorder;
        CameraInput cameraInput;
        
        void Awake () {
            button.onClick.AddListener(ToggleRecording);
        }

        void Update () {
            if (cameraInput == null)
                image.color = Color.white;
            else
                image.color = new Color(1f, 0f, 0f, Mathf.Abs(Mathf.Sin(1.5f * Time.time)));
        }

        void ToggleRecording () {
            if (cameraInput == null)
                StartRecording();
            else
                StopRecording();
        }

        void StartRecording () {
            // Start recording
            recorder = new GIFRecorder(size.x, size.y, 0.1f);
            cameraInput = new CameraInput(recorder, Camera.main);
            cameraInput.frameSkip = 5;
        }

        async void StopRecording () {
            // Stop recording
            cameraInput.Dispose();
            cameraInput = null;
            var path = await recorder.FinishWriting();
            recorder = null;
            // Share
            var payload = new SharePayload();
            payload.AddMedia(path);
            await payload.Commit();
        }
    }
}