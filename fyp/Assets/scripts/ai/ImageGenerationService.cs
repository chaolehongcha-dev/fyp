using UnityEngine;
using UnityEngine.Networking; // 用于 UnityWebRequest
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic; // 用于 List

// ####################################################################
// ## 图像生成服务 (Image Generation Service)
// ####################################################################
public class ImageGenerationService : MonoBehaviour
{
    public static ImageGenerationService Instance { get; private set; }

    // ## 重要 ##: 
    // 1. 去 Google AI Studio (https://aistudio.google.com/) 获取你的 API 密钥。
    // 2. 把它粘贴到这里。
    private const string apiKey = "AIzaSyCUiw58MO1PRsHxBw-owzCswRjI1pIADVo";

    // Google Imagen 4 的 API 端点
    private const string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/imagen-4.0-generate-001:predict?key=";

    // --- 内部用于解析响应 (Response) 的辅助类 ---
    [System.Serializable]
    private class ImagenResponse
    {
        public List<Prediction> predictions;
    }
    [System.Serializable]
    private class Prediction
    {
        public string bytesBase64Encoded;
    }
    // --- (辅助类结束) ---

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 公开方法：请求生成一张结局图片
    /// </summary>
    /// <param name="gameDataJson">来自 EndingManager 的 JSON 字符串</param>
    /// <param name="onComplete">当图片加载完成时调用的回调函数</param>
    public void GenerateEndingImage(string gameDataJson, Action<Texture2D> onComplete)
    {
        StartCoroutine(RequestImageCoroutine(gameDataJson, onComplete));
    }

    private IEnumerator RequestImageCoroutine(string gameDataJson, Action<Texture2D> onComplete)
    {
        Debug.Log("ImageGenerationService: 开始生成结局图片...");

        if (apiKey == "YOUR_GOOGLE_AI_API_KEY_HERE")
        {
            Debug.LogError("ImageGenerationService: API 密钥未设置! 请在 ImageGenerationService.cs 中设置 apiKey。");
            yield break;
        }

        // 1. 创建我们的 "Prompt" (提示词)
        // 我们把你的 JSON 数据作为上下文，让 AI 去理解
        string prompt = "Generate a symbolic, digital art, cinematic ending scene for a video game based on the following player choices (do not include text in the image): " + gameDataJson;

        // 2. 准备 API 请求体 (Payload)
        // 我们需要手动构建 JSON 字符串，因为 Unity 的 JsonUtility 无法处理这种复杂结构

        // 简单地转义 JSON 数据中的引号和换行符
        prompt = prompt.Replace("\"", "\\\"").Replace("\n", "\\n");

        string jsonRequestData = $@"{{
            ""instances"": [
                {{ ""prompt"": ""{prompt}"" }}
            ],
            ""parameters"": {{
                ""sampleCount"": 1
            }}
        }}";

        // 3. 设置 UnityWebRequest
        using (UnityWebRequest www = new UnityWebRequest(apiUrl + apiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // 4. 发送请求并等待
            yield return www.SendWebRequest();

            // 5. 处理结果
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("ImageGenerationService Error: " + www.error);
                Debug.LogError("Response Body: " + www.downloadHandler.text);
            }
            else
            {
                Debug.Log("ImageGenerationService: 成功收到 API 响应。");
                try
                {
                    // 6. 解析响应
                    string jsonResponse = www.downloadHandler.text;
                    ImagenResponse response = JsonUtility.FromJson<ImagenResponse>(jsonResponse);

                    if (response == null || response.predictions == null || response.predictions.Count == 0)
                    {
                        Debug.LogError("ImageGenerationService Error: 响应中未找到 'predictions'。");
                        yield break;
                    }

                    // 7. 解码 Base64 图像
                    string base64Data = response.predictions[0].bytesBase64Encoded;
                    byte[] imageData = Convert.FromBase64String(base64Data);

                    // 8. 创建 Texture2D
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(imageData); // LoadImage 会自动调整纹理大小

                    Debug.Log("ImageGenerationService: 图像解码成功!");

                    // 9. 通过回调函数返回图像
                    onComplete?.Invoke(texture);
                }
                catch (Exception e)
                {
                    Debug.LogError("ImageGenerationService Error: 解析响应失败 - " + e.Message);
                }
            }
        }
    }
}