/* 
*   Detector Cam
*   Copyright (c) 2021 Yusuf Olokoba.
*/

namespace NatSuite.Examples {

    using System.Collections.Generic;
    using UnityEngine;
    using Devices;
    using ML;
    using ML.Vision;
    using ML.Visualizers;
    using ML.Extensions;

    public class DetectorCam : MonoBehaviour {

        [Header("ML")]
        public MLModelData modelData;

        [Header("Visualization")]
        public MLDetectionVisualizer visualizer;

        MLModel model;
        MLAsyncPredictor<(string, Rect, float)[]> predictor;
        CameraDevice cameraDevice;
        Texture2D previewTexture;

        async void Start () {
            // Request camera permissions
            if (!await MediaDeviceQuery.RequestPermissions<CameraDevice>()) {
                Debug.LogError(@"User did not grant camera permissions");
                return;
            }
            // Create async predictor
            model = modelData.Deserialize();
            predictor = new TinyYOLOv3Predictor(model, modelData.labels).ToAsync();
            // Get the default camera device
            var query = new MediaDeviceQuery(MediaDeviceCriteria.CameraDevice);
            cameraDevice = query.current as CameraDevice;
            // Start the camera preview
            cameraDevice.previewResolution = (1280, 720);
            previewTexture = await cameraDevice.StartRunning();
            // Display the camera preview
            visualizer.Render(previewTexture);
        }

        async void Update () {
            // Check if the camera has started
            if (!previewTexture)
                return;
            // Check if the predictor is ready
            if (!predictor.readyForPrediction)
                return;
            // Predict
            var detections = await predictor.Predict(previewTexture);
            // Visualize
            var visualizations = new List<(Rect, string)>();
            foreach (var (label, rect, confidence) in detections) {
                var visualizationText = $"{label}: {confidence:0.##}";
                visualizations.Add((rect, visualizationText));
            }
            visualizer.Render(previewTexture, visualizations.ToArray());
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