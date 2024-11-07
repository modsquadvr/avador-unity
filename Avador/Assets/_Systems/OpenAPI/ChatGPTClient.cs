using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;


public class ChatGPTClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;

#if UNITY_EDITOR
    private const string debugColor = "#1cc950";
#endif

    async void Start()
    {

#if UNITY_EDITOR
        print($"[CLIENT] <color={debugColor}> ChatGPTClient Starting ... </color>");
#endif
        while (!ChatGPTServer.isReady)
        {
            await Task.Delay(500);

#if UNITY_EDITOR
            print($"[CLIENT] <color={debugColor}> Server is NOT ready </color>");
#endif
        }

        try
        {
            client = new TcpClient("127.0.0.1", ChatGPTServer.port);
            stream = client.GetStream();

            string messageToSend = "Hello ChatGPT";
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);

            byte[] buffer = new byte[1024];
            int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, byteCount);
            // OpenAIResponse response_json = JsonConvert.DeserializeObject<OpenAIResponse>(response);

#if UNITY_EDITOR
            print($"[CLIENT] <color={debugColor}> Received response from server: </color>" + response);
#endif
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Connection error: " + ex.Message);
        }
        finally
        {
            stream?.Close();
            client?.Close();
        }
    }


}