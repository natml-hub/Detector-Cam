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

    public class DetectorCam : MonoBehaviour {

        [Header(@"NatML Hub")]
        public string accessKey;

        [Header(@"Visualization")]
        public SSDLiteVisualizer visualizer;

        CameraDevice cameraDevice;
        Texture2D previewTexture;
        byte[] pixelBuffer;
        MLModelData modelData;
        MLModel model;
        SSDLitePredictor predictor;

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
            cameraDevice.previewResolution = (640, 480);
            previewTexture = await cameraDevice.StartRunning();
            pixelBuffer = previewTexture.GetRawTextureData<byte>().ToArray();
            // Display the camera preview
            visualizer.Render(previewTexture);
            // Fetch the model data
            Debug.Log("Fetching model from NatML Hub");
            modelData = await MLModelData.FromHub("@natsuite/ssd-lite", accessKey);
            // Deserialize the model
            model = modelData.Deserialize();
            // Create the SSD Lite predictor
            predictor = new SSDLitePredictor(model, modelData.labels);
        }

        void Update () {
            // Check that the model has been downloaded
            if (predictor == null)
                return;
            // Update pixel buffer to save on allocating memory
            previewTexture.GetRawTextureData<byte>().CopyTo(pixelBuffer);
            // Create input feature
            var inputFeature = new MLImageFeature(pixelBuffer, previewTexture.width, previewTexture.height);
            (inputFeature.mean, inputFeature.std) = modelData.normalization;
            inputFeature.aspectMode = modelData.aspectMode;
            // Predict
            var detections = predictor.Predict(inputFeature);
            // Visualize
            visualizer.Render(previewTexture, detections);
        }

        void OnDisable () {
            // Dispose the predictor and model
            model?.Dispose();
            // Stop the camera preview
            if (cameraDevice?.running ?? false)
                cameraDevice.StopRunning();
        }
    }
}