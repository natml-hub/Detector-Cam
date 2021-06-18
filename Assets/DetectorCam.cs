/* 
*   Detector Cam
*   Copyright (c) 2021 Yusuf Olokoba.
*/

namespace NatSuite.Examples {

    using UnityEngine;
    using NatSuite.Devices;
    using NatSuite.ML;
    using NatSuite.ML.Features;
    using NatSuite.ML.Vision;
    using NatSuite.ML.Visualizers;
    using NatSuite.ML.Extensions;

    public class DetectorCam : MonoBehaviour {

        [Header("Visualization")]
        public MLDetectionVisualizer visualizer;

        CameraDevice cameraDevice;
        Texture2D previewTexture;
        MLModelData modelData;
        MLModel model;
        MLAsyncPredictor<(Rect, string, float)[]> predictor;

        async void Start () {
            // Request camera permissions
            if (!await MediaDeviceQuery.RequestPermissions<CameraDevice>()) {
                Debug.LogError(@"User did not grant camera permissions");
                return;
            }
            // Get the default camera device
            var query = new MediaDeviceQuery(MediaDeviceCriteria.CameraDevice);
            cameraDevice = query.current as CameraDevice;
            // Start the camera preview
            cameraDevice.previewResolution = (1280, 720);
            previewTexture = await cameraDevice.StartRunning();
            // Display the camera preview
            visualizer.Render(previewTexture);
            // Fetch the TinyYOLO detector
            Debug.Log("Fetching model from NatML Hub");
            modelData = await MLModelData.FromHub("@natsuite/tiny-yolo-v3");
            model = modelData.Deserialize();
            predictor = new TinyYOLOv3Predictor(model, modelData.labels).ToAsync();
        }

        async void Update () {
            // Check that the model has been downloaded
            if (predictor == null)
                return;
            // Check if the predictor is ready
            if (!predictor.readyForPrediction)
                return;
            // Predict
            var input = new MLImageFeature(previewTexture);
            (input.mean, input.std) = modelData.normalization;
            input.aspectMode = modelData.aspectMode;
            var detections = await predictor.Predict(input);
            // Visualize
            visualizer.Render(previewTexture, detections);
        }

        void OnDisable () {
            // Dispose the predictor and model
            predictor?.Dispose();
            model?.Dispose();
            // Stop the camera preview
            if (cameraDevice?.running ?? false)
                cameraDevice.StopRunning();
        }
    }
}