/*
using System.Collections.Generic;
using UnityEngine;

namespace Orbits
{
    public enum ImageFormat
    {
        Unknown, Jpeg, Png
    }

    public class Screenshot
    {
        #region Fields
        public static readonly Screenshot Empty = new Screenshot(ImageFormat.Unknown, new byte[0]);

        public readonly ImageFormat Format;
        public readonly byte[] Data;

        public bool IsEmpty => this.Format == ImageFormat.Unknown || this.Data.Length < 4;
        #endregion

        #region Constructor
        public Screenshot(ImageFormat format, byte[] data)
        {
            this.Format = format;
            this.Data = data;
        }
        #endregion
    }

    public class ScreenshotManager : MonoBehaviour
    {
        #region Fields
        public static ScreenshotManager Instance { get; private set; }
        public RenderTexture Texture;
        public Camera MainCamera;
        #endregion

        #region Unity Methods
        void Awake()
        {
            Instance = this;

            this.MainCamera.enabled = false;
            this.FarCamera.enabled = false;
        }
        #endregion

        #region Methods
        public Screenshot TakeScreenshot()
        {
            try
            {
                var mainCameraPosition = Camera.main.transform.position;
                var mainCameraRotation = Camera.main.transform.rotation;

                this.MainCamera.enabled = true;
                this.FarCamera.enabled = true;

                this.MainCamera.transform.position = mainCameraPosition;
                this.MainCamera.transform.rotation = mainCameraRotation;

                RenderTexture.active = this.Texture;

                this.FarCamera.Render();
                this.MainCamera.Render();

                var texture = new Texture2D(this.Texture.width, this.Texture.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, this.Texture.width, this.Texture.height), 0, 0);

                var bytes = texture.EncodeToJPG(85);
                return new Screenshot(ImageFormat.Jpeg, bytes);
            }
            catch (System.Exception exp)
            {
                Debug.LogWarning($"Error taking screenshot: {exp.Message}");
            }
            finally
            {
                this.MainCamera.enabled = false;
                this.FarCamera.enabled = false;
                RenderTexture.active = null;
            }

            return Screenshot.Empty;
        }
        #endregion
    }
}
*/