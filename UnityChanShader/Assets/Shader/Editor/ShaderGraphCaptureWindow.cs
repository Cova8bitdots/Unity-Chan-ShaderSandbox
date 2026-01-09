using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ShaderGraphSingleCapture
{
    private static EditorWindow s_target;
    private static string s_outPath;
    private static int s_scale;
    private static int s_step;
    private static Rect s_originalPos;

    [MenuItem("Tools/ShaderGraph/Capture Active ShaderGraph Window (PNG)")]
    public static void CaptureActive()
    {
        var w = FindActiveShaderGraphWindow();
        if (w == null)
        {
            EditorUtility.DisplayDialog(
                "ShaderGraph Capture",
                "Shader Graphウィンドウが見つかりません。\nShader Graphをアクティブ（フォーカス）にしてから実行してください。",
                "OK");
            return;
        }

        string defaultName = MakeSafeFileName(w.titleContent.text);
        if (string.IsNullOrEmpty(defaultName)) defaultName = "ShaderGraph";
        defaultName += ".png";

        string path = EditorUtility.SaveFilePanel(
            "Save ShaderGraph PNG",
            Application.dataPath,
            defaultName,
            "png");

        if (string.IsNullOrEmpty(path))
            return;

        // 高解像度：ウィンドウ自体を拡大して撮る（2～4推奨）
        int scale = 3;

        StartCapture(w, path, scale);
    }

    private static EditorWindow FindActiveShaderGraphWindow()
    {
        var focused = EditorWindow.focusedWindow;
        if (focused != null && IsShaderGraphWindow(focused))
            return focused;

        return Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(IsShaderGraphWindow);
    }

    private static bool IsShaderGraphWindow(EditorWindow w)
    {
        var t = w.GetType();
        var full = t.FullName ?? "";
        if (full.IndexOf("ShaderGraph", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var title = w.titleContent.text ?? "";
        if (title.IndexOf("Shader Graph", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return false;
    }

    private static void StartCapture(EditorWindow w, string outPath, int scale)
    {
        EditorApplication.update -= OnUpdate;

        s_target = w;
        s_outPath = outPath;
        s_scale = Mathf.Clamp(scale, 1, 6);
        s_step = 0;

        s_target.Show();
        s_target.Focus();

        EditorApplication.update += OnUpdate;
    }

    private static void OnUpdate()
    {
        if (s_target == null)
        {
            Cleanup();
            return;
        }

        // 0) 位置保存 & 拡大 & Repaint
        if (s_step == 0)
        {
            s_originalPos = s_target.position;

            var p = s_originalPos;
            p.width = Mathf.Max(600f, p.width * s_scale);
            p.height = Mathf.Max(400f, p.height * s_scale);
            s_target.position = p;

            s_target.Repaint();
            s_step = 1;
            return;
        }

        // 1) もう1フレーム待つ（ここが“灰色回避”の肝）
        if (s_step == 1)
        {
            s_target.Repaint();
            s_step = 2;
            return;
        }

        // 2) RTへスクショ → ReadPixelsで切り出し → PNG保存
        if (s_step == 2)
        {
            try
            {
                // 画面全体をRTへ（Edit ModeでもOK）
                var rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);

                RenderTexture.active = rt;

                // ウィンドウ矩形（Editor座標）→ ピクセル座標へ（Retina/DPI対応）
                float ppp = EditorGUIUtility.pixelsPerPoint;
                var pos = s_target.position;

                int x = Mathf.RoundToInt(pos.x * ppp);
                int yFromTop = Mathf.RoundToInt(pos.y * ppp);
                int w = Mathf.RoundToInt(pos.width * ppp);
                int h = Mathf.RoundToInt(pos.height * ppp);

                // ReadPixelsは左下原点
                int y = Screen.height - yFromTop - h;

                // クランプ
                x = Mathf.Clamp(x, 0, Screen.width - 1);
                y = Mathf.Clamp(y, 0, Screen.height - 1);
                w = Mathf.Clamp(w, 1, Screen.width - x);
                h = Mathf.Clamp(h, 1, Screen.height - y);

                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(x, y, w, h), 0, 0);
                tex.Apply(false, false);

                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                File.WriteAllBytes(s_outPath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);

                // 戻す
                s_target.position = s_originalPos;
                s_target.Repaint();

                EditorUtility.RevealInFinder(s_outPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                // 戻す
                s_target.position = s_originalPos;
                s_target.Repaint();

                EditorUtility.DisplayDialog(
                    "ShaderGraph Capture",
                    "キャプチャに失敗しました。Consoleを確認してください。\n" +
                    "※ウィンドウが他のウィンドウに隠れていると欠けることがあります。",
                    "OK");
            }
            finally
            {
                Cleanup();
            }
        }
    }

    private static void Cleanup()
    {
        EditorApplication.update -= OnUpdate;
        s_target = null;
        s_outPath = null;
        s_scale = 1;
        s_step = 0;
    }

    private static string MakeSafeFileName(string s)
    {
        if (string.IsNullOrEmpty(s)) return "ShaderGraph";
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s;
    }
}
