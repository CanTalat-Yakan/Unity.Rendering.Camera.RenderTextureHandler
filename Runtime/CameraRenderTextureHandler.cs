using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace UnityEssentials
{
    [Serializable]
    public class DisplaySetterData
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

        [SerializeField] private DisplaySetterData _data;

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

            var ui = PrefabSpawner.Instantiate("UnityEssentials_Camera_UIDocument", "Camera UIDocument", this.gameObject.transform);
            if (ui != null)
            {
                var links = ui.GetComponentsInChildren<UIElementLink>();
                _panel = links[0];
                _image = links[1];
            }
        }

        public void Update()
        {
            if (_data.RenderWidth != _lastRenderSize.x ||
                _data.RenderHeight != _lastRenderSize.y ||
                _data.AspectRatio != _lastAspectRatio ||
                _data.FilterMode != _lastFilterMode ||
                _data.HighDynamicRange != _lastHighDynamicRange)
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

            _lastRenderSize.x = _data.RenderWidth;
            _lastRenderSize.y = _data.RenderHeight;
            _lastAspectRatio = _data.AspectRatio;
            _lastFilterMode = _data.FilterMode;
            _lastHighDynamicRange = _data.HighDynamicRange;
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
                name = $"Camera RenderTexture @{(int)targetWidth}x{(int)targetHeight} {(_data.HighDynamicRange ? "HDR" : "SRGB")} {_data.FilterMode}",
                antiAliasing = 1,
                anisoLevel = 0,
                useMipMap = _data.UseMipMap,
                autoGenerateMips = _data.UseMipMap,
                filterMode = _data.FilterMode,
                graphicsFormat = _data.HighDynamicRange
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
                targetWidth = _data.RenderHeight * sourceAspectRatio;
                targetHeight = _data.RenderHeight;
            }
            else
            {
                // Image is taller than the target aspect ratio, adjust height
                targetWidth = _data.RenderWidth;
                targetHeight = _data.RenderWidth / sourceAspectRatio;
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
            if (_data.RenderWidth < 100)
                _data.RenderWidth = 100;

            if (_data.RenderHeight < 100)
                _data.RenderHeight = 100;
        }

        private void GetAspectRatios(out float screenAspectRatio, out float correctedAspectRatio, out float renderAspectRatio)
        {
            screenAspectRatio = (float)Screen.width / Screen.height;
            correctedAspectRatio = _data.AspectRatio == 0 ? screenAspectRatio : _data.AspectRatio;
            renderAspectRatio = (float)_data.RenderWidth / _data.RenderHeight;
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