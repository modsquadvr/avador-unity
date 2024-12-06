using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using UnityEngine;

public class ChatGPTServer : MonoBehaviour
{

    public static bool isReady;
    public const int port = 13000;

#if UNITY_EDITOR
    private const string debugColor = "#1ca7c9";
#endif

    private const string DebugResponseObject = "{\"id\":\"chatcmpl-7xy7BzIcO09A1MKqD3mZG09VlT6Df\",\"object\":\"chat.completion\",\"created\":1699476223,\"model\":\"gpt-3.5-turbo\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"Hello! How can I assist you today?\"},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":9,\"total_tokens\":19}}";

    void Awake()
    {
        Task.Run(() => StartServer());
    }

    void OnDestroy()
    {
        listener.Stop();
        isReady = false;
    }

    private static TcpListener listener;
    public static async Task StartServer()
    {
        listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        isReady = true;

#if UNITY_EDITOR
        print($"[SERVER] <color={debugColor}>TCP Server started on port {port}...</color>");
#endif
        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();

        byte[] buffer = new byte[1024];
        int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
        string userMessage = Encoding.UTF8.GetString(buffer, 0, byteCount);

#if UNITY_EDITOR
        print($"[SERVER] <color={debugColor}>Received message: </color>" + userMessage);
#endif
        string chatGPTResponse = await GetChatGPTResponseAsync(userMessage);

        byte[] responseBytes = Encoding.UTF8.GetBytes(chatGPTResponse);
        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);

        client.Close();
    }

    private static async Task<string> GetChatGPTResponseAsync(string prompt, bool isDebug = true)
    {
        if (isDebug)
            return "(debug) Hello! How can I assist you today?";

        using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) })
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Secret.API_KEY}");

            var request = new OpenAIRequest
            {
                model = "gpt-3.5-turbo",
                messages = new[] { new Message { role = "user", content = prompt } }
            };

            string jsonRequest = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Debug.LogError($"[SERVER] Recieved Status Code {response.StatusCode}");
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();

            OpenAIResponse openAIResponse = JsonConvert.DeserializeObject<OpenAIResponse>(jsonResponse);

            // string chatGPTResponse = openAIResponse.choices.Length > 0 ? openAIResponse.choices[0].message.content : "Error: No response from ChatGPT.";
            return openAIResponse.GetContent();
        }
    }
}


[Serializable]
public class OpenAIRequest
{
    public string model;
    public Message[] messages;
}
