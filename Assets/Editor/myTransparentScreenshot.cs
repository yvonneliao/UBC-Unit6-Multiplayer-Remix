using System.IO;
using UnityEditor;
using UnityEngine;

public static class myTransparentScreenshot
{
    [MenuItem("Tools/Export Transparent Game View PNG")]
    public static void _exportPng()
    {
        Camera _camera = Camera.main;
        if (_camera == null)
        {
            Debug.LogError("No Main Camera found.");
            return;
        }

        int _width = 1920;
        int _height = 1080;

        string _path = EditorUtility.SaveFilePanel(
            "Save Transparent PNG",
            "",
            "SplashScreen.png",
            "png");

        if (string.IsNullOrEmpty(_path))
            return;

        RenderTexture _rt = new RenderTexture(_width, _height, 24, RenderTextureFormat.ARGB32);
        RenderTexture _prevTarget = _camera.targetTexture;
        RenderTexture _prevActive = RenderTexture.active;

        _camera.targetTexture = _rt;
        _camera.Render();

        RenderTexture.active = _rt;
        Texture2D _tex = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        _tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
        _tex.Apply();

        _camera.targetTexture = _prevTarget;
        RenderTexture.active = _prevActive;
        Object.DestroyImmediate(_rt);

        File.WriteAllBytes(_path, _tex.EncodeToPNG());
        Object.DestroyImmediate(_tex);

        Debug.Log($"Saved transparent PNG to: {_path}");
    }
}
