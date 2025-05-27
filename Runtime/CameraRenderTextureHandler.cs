using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace UnityEssentials
{
    [Serializable]
    public class RenderTextureSettings
    {
        public int RenderWidth = 1920;
        public int RenderHeight = 1080;
        public float AspectRatio = 16 / 9f;
        public FilterMode FilterMode = FilterMode.Point;
        public bool HighDynamicRange = false;
        public bool UseMipMap = false;
    }

    [RequireComponent(typeof(Camera))]
    public partial class CameraRenderTextureHandler : MonoBehaviour
    {
        public static RenderTexture RenderTexture { get; private set; }

        public RenderTextureSettings RenderTextureSettings;

        private Camera _camera;
        private UIElementLink _image;
        private UIElementLink _panel;

        private RenderTexture _renderTexture;

        private bool _isDirty;

        private Vector2Int _lastScreenSize;
        private Vector2Int _lastRenderSize;
        private FilterMode _lastFilterMode;
        private float _lastAspectRatio;
        private bool _lastHighDynamicRange;

        public void Awake()
        {
            _camera = GetComponent<Camera>();

            var prefab = ResourceLoader.InstantiatePrefab("UnityEssentials_Camera_UIDocument", "Camera UI Document", this.gameObject.transform);
            if (prefab != null)
            {
                var links = prefab.GetComponentsInChildren<UIElementLink>();
                _panel = links[0];
                _image = links[1];
            }
        }

        public void Update()
        {
            if (RenderTextureSettings.RenderWidth != _lastRenderSize.x ||
                RenderTextureSettings.RenderHeight != _lastRenderSize.y ||
                RenderTextureSettings.AspectRatio != _lastAspectRatio ||
                RenderTextureSettings.FilterMode != _lastFilterMode ||
                RenderTextureSettings.HighDynamicRange != _lastHighDynamicRange)
            {
                _isDirty = true;
            }

            if (_isDirty && Application.isPlaying)
                UpdateChanges();

            if (Screen.width != _lastScreenSize.x ||
                Screen.height != _lastScreenSize.y)
            {
                UpdateScreenSize();
                AdjustAspectRatio();
            }

            _lastRenderSize.x = RenderTextureSettings.RenderWidth;
            _lastRenderSize.y = RenderTextureSettings.RenderHeight;
            _lastAspectRatio = RenderTextureSettings.AspectRatio;
            _lastFilterMode = RenderTextureSettings.FilterMode;
            _lastHighDynamicRange = RenderTextureSettings.HighDynamicRange;
        }

        [ContextMenu("Update")]
        public void UpdateChanges()
        {
            InitializeRenderTexture();
            InitializeUIDocument();
            UpdateScreenSize();
            AdjustAspectRatio();

            _isDirty = false;
        }
    }

    public partial class CameraRenderTextureHandler : MonoBehaviour
    {
        private void InitializeRenderTexture()
        {
            Cleanup();

            ValidateRenderTextureSize();

            GetAspectRatios(out var screenAspectRatio, out var correctedAspectRatio, out var renderAspectRatio);
            AdjustAspectRatioSize(correctedAspectRatio, renderAspectRatio, out var targetWidth, out var targetHeight);

            _renderTexture = new RenderTexture((int)targetWidth, (int)targetHeight, 0)
            {
                name = $"Camera RenderTexture @{(int)targetWidth}x{(int)targetHeight} {(RenderTextureSettings.HighDynamicRange ? "HDR" : "SRGB")} {RenderTextureSettings.FilterMode}",
                antiAliasing = 1,
                anisoLevel = 0,
                useMipMap = RenderTextureSettings.UseMipMap,
                autoGenerateMips = RenderTextureSettings.UseMipMap,
                filterMode = RenderTextureSettings.FilterMode,
                graphicsFormat = RenderTextureSettings.HighDynamicRange
                    ? GraphicsFormat.R16G16B16A16_SFloat
                    : GraphicsFormat.R8G8B8A8_SRGB
            };
            _renderTexture.Create();

            _camera.targetTexture = _renderTexture;

            RenderTexture = _renderTexture;
        }

        private void InitializeUIDocument()
        {
            if (_image.LinkedElement is VisualElement iamge)
                iamge.SetImage(_renderTexture);
        }

        private void AdjustAspectRatio()
        {
            GetAspectRatios(out var screenAspectRatio, out var correctedAspectRatio, out _);
            GetAspectRatioSizePercentage(screenAspectRatio, correctedAspectRatio, out var panelWidthPercentage, out var panelHeightPercentage);

            if (_panel.LinkedElement is VisualElement panel)
                panel.SetSize(panelWidthPercentage, panelHeightPercentage);
        }
    }

    public partial class CameraRenderTextureHandler : MonoBehaviour
    {
        private void AdjustAspectRatioSize(float sourceAspectRatio, float referenceAspectRatio, out float targetWidth, out float targetHeight)
        {
            if (referenceAspectRatio > sourceAspectRatio)
            {
                // Image is wider than the target aspect ratio, adjust width
                targetWidth = RenderTextureSettings.RenderHeight * sourceAspectRatio;
                targetHeight = RenderTextureSettings.RenderHeight;
            }
            else
            {
                // Image is taller than the target aspect ratio, adjust height
                targetWidth = RenderTextureSettings.RenderWidth;
                targetHeight = RenderTextureSettings.RenderWidth / sourceAspectRatio;
            }
        }

        private void GetAspectRatioSizePercentage(float sourceAspectRatio, float referenceAspectRatio, out float widthPercentage, out float heightPercentage)
        {
            if (referenceAspectRatio > sourceAspectRatio)
            {
                // Wider than the screen (horizontal bars)
                widthPercentage = 100;
                heightPercentage = (sourceAspectRatio / referenceAspectRatio) * 100;
            }
            else
            {
                // Taller than the screen (vertical bars) or square (1:1)
                heightPercentage = 100;
                widthPercentage = (referenceAspectRatio / sourceAspectRatio) * 100;
            }
        }

        private void ValidateRenderTextureSize()
        {
            if (RenderTextureSettings.RenderWidth < 100)
                RenderTextureSettings.RenderWidth = 100;

            if (RenderTextureSettings.RenderHeight < 100)
                RenderTextureSettings.RenderHeight = 100;
        }

        private void GetAspectRatios(out float screenAspectRatio, out float correctedAspectRatio, out float renderAspectRatio)
        {
            screenAspectRatio = (float)Screen.width / Screen.height;
            correctedAspectRatio = RenderTextureSettings.AspectRatio == 0 ? screenAspectRatio : RenderTextureSettings.AspectRatio;
            renderAspectRatio = (float)RenderTextureSettings.RenderWidth / RenderTextureSettings.RenderHeight;
        }

        private void UpdateScreenSize() =>
            _lastScreenSize = new(Screen.width, Screen.height);

        private void Cleanup()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();

                Destroy(_renderTexture);
            }
        }
    }
}