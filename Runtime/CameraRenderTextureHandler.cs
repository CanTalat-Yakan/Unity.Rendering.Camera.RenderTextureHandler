using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEssentials
{
    [Serializable]
    public class RenderTextureSettings
    {
        public int RenderWidth = 1920;
        public int RenderHeight = 1080;

        [Space]
        public float AspectRatioNumerator = 0;
        public float AspectRatioDenominator = 0;

        [Space]
        public FilterMode FilterMode = FilterMode.Point;
        public bool HighDynamicRange = false;
        public bool UseMipMap = false;
    }

    [RequireComponent(typeof(Camera))]
    public partial class CameraRenderTextureHandler : MonoBehaviour
    {
        public static RenderTexture RenderTexture { get; private set; }

        public RenderTextureSettings Settings;

        private Camera _camera;
        private UIElementLink _panel;
        private UIElementLink _image;

        private RenderTexture _renderTexture;

        private bool _isDirty;

        private Vector2Int _lastScreenSize;
        private Vector2Int _lastRenderSize;
        private FilterMode _lastFilterMode;
        private float _lastAspectRatioNumerator;
        private float _lastAspectRatioDenominator;
        private bool _lastHighDynamicRange;

        public void Awake()
        {
            _camera = GetComponent<Camera>();

            var prefab = AssetResolver.InstantiatePrefab("UnityEssentials_Camera_UIDocument", "UI Document", this.gameObject.transform);
            if (prefab != null)
            {
                _panel = prefab.transform.Find("VisualElement (AspectRatio)")?.GetComponent<UIElementLink>();
                _image = prefab.transform.Find("VisualElement (RenderTexture)")?.GetComponent<UIElementLink>();
            }
        }

        public void Update()
        {
            if (Settings.RenderWidth != _lastRenderSize.x ||
                Settings.RenderHeight != _lastRenderSize.y ||
                Settings.AspectRatioNumerator != _lastAspectRatioNumerator ||
                Settings.AspectRatioDenominator != _lastAspectRatioDenominator ||
                Settings.FilterMode != _lastFilterMode ||
                Settings.HighDynamicRange != _lastHighDynamicRange)
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

            _lastRenderSize.x = Settings.RenderWidth;
            _lastRenderSize.y = Settings.RenderHeight;
            _lastAspectRatioNumerator = Settings.AspectRatioNumerator;
            _lastAspectRatioDenominator = Settings.AspectRatioDenominator;
            _lastFilterMode = Settings.FilterMode;
            _lastHighDynamicRange = Settings.HighDynamicRange;

            if (RenderTexture == null)
                UpdateChanges();
        }

        [ContextMenu("Update")]
        public void UpdateChanges()
        {
            InitializeRenderTexture();
            SendRenderRequest();
            InitializeUIDocument();
            UpdateScreenSize();
            AdjustAspectRatio();

            _isDirty = false;
        }

        private void SendRenderRequest()
        {
            var request = new RenderPipeline.StandardRequest();

            if (RenderPipeline.SupportsRenderRequest(_camera, request))
            {
                request.destination = _camera.targetTexture;
                RenderPipeline.SubmitRenderRequest(_camera, request);
            }
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
                name = $"Camera RenderTexture @{(int)targetWidth}x{(int)targetHeight} {(Settings.HighDynamicRange ? "HDR" : "SRGB")} {Settings.FilterMode}",
                antiAliasing = 1,
                anisoLevel = 0,
                useMipMap = Settings.UseMipMap,
                autoGenerateMips = Settings.UseMipMap,
                filterMode = Settings.FilterMode,
                graphicsFormat = Settings.HighDynamicRange
                    ? GraphicsFormat.B10G11R11_UFloatPack32
                    : GraphicsFormat.R8G8B8A8_SRGB
            };
            _renderTexture.Create();

            _camera.targetTexture = _renderTexture;

            RenderTexture = _renderTexture;
        }

        private void InitializeUIDocument()
        {
            if (_image.LinkedElement is VisualElement image)
                image.SetBackgroundImage(_renderTexture);
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
                targetWidth = Settings.RenderHeight * sourceAspectRatio;
                targetHeight = Settings.RenderHeight;
            }
            else
            {
                // Image is taller than the target aspect ratio, adjust height
                targetWidth = Settings.RenderWidth;
                targetHeight = Settings.RenderWidth / sourceAspectRatio;
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
            if (Settings.RenderWidth < 100)
                Settings.RenderWidth = 100;

            if (Settings.RenderHeight < 100)
                Settings.RenderHeight = 100;
        }

        private void GetAspectRatios(out float screenAspectRatio, out float correctedAspectRatio, out float renderAspectRatio)
        {
            var aspectRatio = 0f;
            if (Settings.AspectRatioNumerator != 0 && Settings.AspectRatioDenominator != 0)
                aspectRatio = Mathf.Clamp(Settings.AspectRatioNumerator / Settings.AspectRatioDenominator, 0, 100);

            screenAspectRatio = (float)Screen.width / Screen.height;
            correctedAspectRatio = aspectRatio == 0 ? screenAspectRatio : aspectRatio;
            renderAspectRatio = (float)Settings.RenderWidth / Settings.RenderHeight;
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