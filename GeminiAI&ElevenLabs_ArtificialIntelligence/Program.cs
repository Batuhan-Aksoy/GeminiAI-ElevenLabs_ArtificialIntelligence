using GeminiAI_ElevenLabs_ArtificialIntelligence.Models;
using NAudio.Wave;
using System.Speech.Recognition;
using System.Text;
using System.Text.Json;


string geminiAPI_Key = "YourGeminiAPIKey";
string geminiAPI_Url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=";
var geminiClient = new HttpClient();
var elevenLabsClient = new HttpClient();


string elevenLabsAPI_Key = "YourElevenLabsAPIKey";
string elevenLabsVoiceId = "YourElevenLabsVoiceId";
string elevenLabsAPI_Url = "https://api.elevenlabs.io/v1/text-to-speech/{0}/stream"; //{voice_id}

elevenLabsClient.DefaultRequestHeaders.Add("xi-api-key", elevenLabsAPI_Key);


var spokenText = "";

try
{
    while (true)
    {

        SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine();

        recognizer.SetInputToDefaultAudioDevice();
        GrammarBuilder grammarBuilder = new GrammarBuilder();
        grammarBuilder.AppendDictation();
        Grammar grammar = new Grammar(grammarBuilder);
        recognizer.LoadGrammar(grammar);
        recognizer.SpeechRecognized += (sender, e) =>
        {
            Console.WriteLine("Recognized text: " + e.Result.Text);
            spokenText = e.Result.Text;
        };
        recognizer.RecognizeAsync(RecognizeMode.Multiple);
        Console.WriteLine("Please speak then press any key to send...");
        Console.ReadKey();
        recognizer.Dispose();


        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("You :" + spokenText);
        Console.ResetColor();


        var geminiRequestModel = new GeminiRequestModel
        {
            contents = new List<ContentModel>
            {
               new ContentModel
               {
                   parts = new List<PartModel>
                   {
                       new PartModel
                       {
                           text = spokenText
                       }
                   }
               }
            }
        };

        var geminiRequestContent = new StringContent(JsonSerializer.Serialize<GeminiRequestModel>(geminiRequestModel), Encoding.UTF8, "application/json");


        var geminiResponse = await geminiClient.PostAsync(geminiAPI_Url + geminiAPI_Key, geminiRequestContent);
        string geminiResponseBodyJson = await geminiResponse.Content.ReadAsStringAsync();

        Console.WriteLine();

        GeminiResponseModel geminiResponseBody = JsonSerializer.Deserialize<GeminiResponseModel>(geminiResponseBodyJson);

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write("Gemini: ");
        Console.ResetColor();
        Console.WriteLine(geminiResponseBody?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text ?? string.Empty);


        string elevenLabsText = geminiResponseBody?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text.Replace('*', ' ') ?? string.Empty;
        var elevenLabsRequestData = new ElevenLabsRequestModel() { text = elevenLabsText };
        var elevenLabsRequestContent = new StringContent(JsonSerializer.Serialize(elevenLabsRequestData), Encoding.UTF8, "application/json");
        var elevenLabsResponse = await elevenLabsClient.PostAsync(string.Format(elevenLabsAPI_Url, elevenLabsVoiceId), elevenLabsRequestContent);

        if (elevenLabsResponse.StatusCode == System.Net.HttpStatusCode.OK)
        {
            byte[] audioBytes = await elevenLabsResponse.Content.ReadAsByteArrayAsync();

            using (MemoryStream memoryStream = new MemoryStream(audioBytes))
            {
                using (WaveStream waveStream = new Mp3FileReader(memoryStream))
                {
                    using (WaveOutEvent waveOutDevice = new WaveOutEvent())
                    {
                        waveOutDevice.Init(waveStream);
                        waveOutDevice.Play();

                        while (waveOutDevice.PlaybackState == PlaybackState.Playing)
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
            }
        }
        else
        {
            string elevenLabsErrorJson = await elevenLabsResponse.Content.ReadAsStringAsync();
            ElevenLabsErrorModel elevenLabsError = JsonSerializer.Deserialize<ElevenLabsErrorModel>(elevenLabsErrorJson);
            Console.WriteLine("Eleven Labs: " + elevenLabsError.detail.message);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}


