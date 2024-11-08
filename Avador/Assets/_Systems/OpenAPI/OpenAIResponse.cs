using System;
[Serializable]
public class OpenAIResponse
{
    public string id;
    public string @object;
    public long created;
    public string model;
    public Choice[] choices;
    public Usage usage;
    public string system_fingerprint;

    [Serializable]
    public class Choice
    {
        public int index;
        public Message message;
        public object logprobs;
        public string finish_reason;
    }

    [Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
        public PromptTokensDetails prompt_tokens_details;
        public CompletionTokensDetails completion_tokens_details;
    }

    [Serializable]
    public class PromptTokensDetails
    {
        public int cached_tokens;
        public int audio_tokens;
    }

    [Serializable]
    public class CompletionTokensDetails
    {
        public int reasoning_tokens;
        public int audio_tokens;
        public int accepted_prediction_tokens;
        public int rejected_prediction_tokens;
    }

    public string GetContent()
    {
        if (choices != null && choices.Length > 0)
        {
            return choices[0].message.content;
        }
        return null;
    }
}

[Serializable]
public class Message
{
    public string role;
    public string content;
    public object refusal;
}