using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Script.Serialization;
using System.Threading.Tasks;

namespace SpeechToText {
    class Program {
        static async Task Main(string[] args) {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            SpeechConfig config = SpeechConfig.FromSubscription("1bd825a94b2f4b74bd50d080da63ca3f", "centralus");

            // Create an audio stream from a wav file.
            // Replace with your own audio file name.
            using (AudioConfig audioInput = Helper.OpenWavFile(@"VoiceEnmanuelle2.wav")) {
                //await TranscribirSimple(config, audioInput);
                //CreateVoiceSignatureByUsingBody().Wait();
            }

            await ConversationWithPullAudioStreamAsync_B();
        }

        static async Task TranscribirSimple(SpeechConfig config, AudioConfig audioInput) {
            TaskCompletionSource<int> stopRecognition = new TaskCompletionSource<int>();

            // Creates a speech recognizer using audio stream input.
            using (SpeechRecognizer recognizer = new SpeechRecognizer(config, "en-US", audioInput)) {
                // Subscribes to events.
                recognizer.Recognizing += (s, e) => {
                    Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                };

                recognizer.Recognized += (s, e) => {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech) {
                        Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch) {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };

                recognizer.Canceled += (s, e) => {
                    Console.WriteLine($"CANCELED: Reason={e.Reason}");

                    if (e.Reason == CancellationReason.Error) {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }

                    stopRecognition.TrySetResult(0);
                };

                recognizer.SessionStarted += (s, e) => {
                    Console.WriteLine("\nSession started event.");
                };

                recognizer.SessionStopped += (s, e) => {
                    Console.WriteLine("\nSession stopped event.");
                    Console.WriteLine("\nStop recognition.");
                    stopRecognition.TrySetResult(0);
                };

                // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                // Waits for completion.
                // Use Task.WaitAny to keep the task rooted.
                //Task.WaitAny(new[] { stopRecognition.Task });
                Task.WaitAny(new[] { stopRecognition.Task });

                // Stops recognition.
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

                Console.ReadLine();
            }
        }

        static async Task ConversationWithPullAudioStreamAsync() {
            // Creates an instance of a speech config with specified subscription key and service region
            // Replace with your own subscription key and region
            var config = SpeechConfig.FromSubscription("1bd825a94b2f4b74bd50d080da63ca3f", "centralus");
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
            var stopTranscription = new TaskCompletionSource<int>();

            // Create an audio stream from a wav file or from the default microphone if you want to stream live audio from the supported devices
            // Replace with your own audio file name and Helper class which implements AudioConfig using PullAudioInputStreamCallback
            //using (var audioInput = Helper.OpenWavFile(@"VoiceEnmanuelle2.wav")) {
            using (var audioInput = AudioConfig.FromWavFileInput(@"katiesteve.wav")) {
                var meetingId = Guid.NewGuid().ToString();
                using (var conversation = await Conversation.CreateConversationAsync(config, meetingId).ConfigureAwait(false)) {
                    // Create a conversation transcriber using audio stream input
                    using (var conversationTranscriber = new ConversationTranscriber(audioInput)) {
                        await conversationTranscriber.JoinConversationAsync(conversation);

                        // Subscribe to events
                        //conversationTranscriber.Transcribing += (s, e) => {
                        //    Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text}");
                        //};

                        conversationTranscriber.Transcribed += (s, e) => {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech) {
                                var a = e.Result;
                                Console.WriteLine($"TRANSCRIBED: {e.Result.UserId}: Text={e.Result.Text}");
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch) {
                                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                            }
                        };

                        conversationTranscriber.Canceled += (s, e) => {
                            Console.WriteLine($"CANCELED: Reason={e.Reason}");

                            if (e.Reason == CancellationReason.Error) {
                                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                                Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                                Console.WriteLine($"CANCELED: Did you update the subscription info?");
                                stopTranscription.TrySetResult(0);
                            }
                        };

                        conversationTranscriber.SessionStarted += (s, e) => {
                            Console.WriteLine("\nSession started event.");
                        };

                        conversationTranscriber.SessionStopped += (s, e) => {
                            Console.WriteLine("\nSession stopped event.");
                            Console.WriteLine("\nStop recognition.");
                            stopTranscription.TrySetResult(0);
                        };

                        // Add participants to the conversation.
                        // Create voice signatures using REST API described in the earlier section in this document.
                        // Voice signature needs to be in the following format:
                        // { "Version": <Numeric string or integer value>, "Tag": "string", "Data": "string" }

                        string signatureKatie = await CreateVoiceSignatureByUsingBodyKatie().ConfigureAwait(false);
                        string signatureSteve = await CreateVoiceSignatureByUsingBodySteve().ConfigureAwait(false);

                        Participant speakerKatie = Participant.From("Katie", "en-us", signatureKatie);
                        Participant speakerSteve = Participant.From("Steve", "en-us", signatureSteve);
                        await conversation.AddParticipantAsync(speakerKatie).ConfigureAwait(false);
                        await conversation.AddParticipantAsync(speakerSteve).ConfigureAwait(false);

                        // Starts transcribing of the conversation. Uses StopTranscribingAsync() to stop transcribing when all participants leave.
                        await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

                        // Waits for completion.
                        // Use Task.WaitAny to keep the task rooted.
                        Task.WaitAny(new[] { stopTranscription.Task });

                        // Stop transcribing the conversation.
                        await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);
                    }
                }
            }

            Console.ReadLine();
        }
        static async Task ConversationWithPullAudioStreamAsync_B() {
            // Creates an instance of a speech config with specified subscription key and service region
            // Replace with your own subscription key and region
            var config = SpeechConfig.FromSubscription("1bd825a94b2f4b74bd50d080da63ca3f", "centralus");
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
            config.SetServiceProperty("transcriptionMode", "async", ServicePropertyChannel.UriQueryParameter);
            var stopTranscription = new TaskCompletionSource<int>();

            // Create an audio stream from a wav file or from the default microphone if you want to stream live audio from the supported devices
            // Replace with your own audio file name and Helper class which implements AudioConfig using PullAudioInputStreamCallback
            //using (var audioInput = Helper.OpenWavFile(@"VoiceEnmanuelle2.wav")) {
            using (var audioInput = AudioConfig.FromWavFileInput(@"katiesteve.wav")) {
                var meetingId = Guid.NewGuid().ToString();
                using (var conversation = await Conversation.CreateConversationAsync(config, meetingId).ConfigureAwait(false)) {
                    // Create a conversation transcriber using audio stream input
                    using (var conversationTranscriber = new ConversationTranscriber(audioInput)) {
                        await conversationTranscriber.JoinConversationAsync(conversation);

                        // Subscribe to events
                        //conversationTranscriber.Transcribing += (s, e) => {
                        //    Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text}");
                        //};

                        //conversationTranscriber.Transcribed += (s, e) => {
                        //    if (e.Result.Reason == ResultReason.RecognizedSpeech) {
                        //        var a = e.Result;
                        //        Console.WriteLine($"TRANSCRIBED: {e.Result.UserId}: Text={e.Result.Text}");
                        //    }
                        //    else if (e.Result.Reason == ResultReason.NoMatch) {
                        //        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        //    }
                        //};
                        
                        conversationTranscriber.Transcribed += (s, e) => {
                            Console.WriteLine($"TRANSCRIBED: {e.Result.Text}");
                        };

                        conversationTranscriber.Canceled += (s, e) => {
                            Console.WriteLine($"CANCELED: Reason={e.Reason}");

                            if (e.Reason == CancellationReason.Error) {
                                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                                Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                                Console.WriteLine($"CANCELED: Did you update the subscription info?");
                                stopTranscription.TrySetResult(0);
                            }
                        };

                        conversationTranscriber.SessionStarted += (s, e) => {
                            Console.WriteLine("\nSession started event.");
                        };

                        conversationTranscriber.SessionStopped += (s, e) => {
                            Console.WriteLine("\nSession stopped event.");
                            Console.WriteLine("\nStop recognition.");
                            stopTranscription.TrySetResult(0);
                        };

                        // Add participants to the conversation.
                        // Create voice signatures using REST API described in the earlier section in this document.
                        // Voice signature needs to be in the following format:
                        // { "Version": <Numeric string or integer value>, "Tag": "string", "Data": "string" }

                        string signatureKatie = await CreateVoiceSignatureByUsingBodyKatie().ConfigureAwait(false);
                        string signatureSteve = await CreateVoiceSignatureByUsingBodySteve().ConfigureAwait(false);

                        Participant speakerKatie = Participant.From("Katie", "en-us", signatureKatie);
                        Participant speakerSteve = Participant.From("Steve", "en-us", signatureSteve);
                        await conversation.AddParticipantAsync(speakerKatie).ConfigureAwait(false);
                        await conversation.AddParticipantAsync(speakerSteve).ConfigureAwait(false);

                        // Starts transcribing of the conversation. Uses StopTranscribingAsync() to stop transcribing when all participants leave.
                        await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

                        // Waits for completion.
                        // Use Task.WaitAny to keep the task rooted.
                        Task.WaitAny(new[] { stopTranscription.Task });

                        // Stop transcribing the conversation.
                        await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);
                    }
                }
            }

            Console.ReadLine();
        }

        static async Task<string> CreateVoiceSignatureByUsingBodyKatie() {
            // Replace with your own region
            var region = "centralus";
            // Change the name of the wave file to match yours
            byte[] fileBytes = File.ReadAllBytes(@"enrollment_audio_katie.wav");
            var content = new ByteArrayContent(fileBytes);

            var client = new HttpClient();
            // Add your subscription key to the header Ocp-Apim-Subscription-Key directly
            // Replace "YourSubscriptionKey" with your own subscription key
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "1bd825a94b2f4b74bd50d080da63ca3f");
            // Edit with your desired region for `{region}`
            var response = await client.PostAsync($"https://signature.{region}.cts.speech.microsoft.com/api/v1/Signature/GenerateVoiceSignatureFromByteArray", content);
            // A voice signature contains Version, Tag and Data key values from the Signature json structure from the Response body.
            // Voice signature format example: { "Version": <Numeric string or integer value>, "Tag": "string", "Data": "string" }
            var jsonData = await response.Content.ReadAsStringAsync();

            JSONData Modelo = new JSONData();
            Modelo = JsonConvert.DeserializeObject<JSONData>(jsonData);

            string signature = new JavaScriptSerializer().Serialize(Modelo.Signature);
            string serializedItem = JsonConvert.SerializeObject(Modelo.Signature);

            return serializedItem;
        }

        static async Task<string> CreateVoiceSignatureByUsingBodySteve() {
            // Replace with your own region
            var region = "centralus";
            // Change the name of the wave file to match yours
            byte[] fileBytes = File.ReadAllBytes(@"enrollment_audio_steve.wav");
            var content = new ByteArrayContent(fileBytes);

            var client = new HttpClient();
            // Add your subscription key to the header Ocp-Apim-Subscription-Key directly
            // Replace "YourSubscriptionKey" with your own subscription key
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "1bd825a94b2f4b74bd50d080da63ca3f");
            // Edit with your desired region for `{region}`
            var response = await client.PostAsync($"https://signature.{region}.cts.speech.microsoft.com/api/v1/Signature/GenerateVoiceSignatureFromByteArray", content);
            // A voice signature contains Version, Tag and Data key values from the Signature json structure from the Response body.
            // Voice signature format example: { "Version": <Numeric string or integer value>, "Tag": "string", "Data": "string" }
            var jsonData = await response.Content.ReadAsStringAsync();

            JSONData Modelo = new JSONData();
            Modelo = JsonConvert.DeserializeObject<JSONData>(jsonData);

            string signature = new JavaScriptSerializer().Serialize(Modelo.Signature);
            string serializedItem = JsonConvert.SerializeObject(Modelo.Signature);

            return serializedItem;
        }

        static async Task<string> CreateVoiceSignatureByUsingFormDataKatie() {
            // Replace with your own region
            var region = "centralus";
            // Change the name of the wave file to match yours
            byte[] fileBytes = File.ReadAllBytes(@"enrollment_audio_katie.wav");
            var form = new MultipartFormDataContent();
            var content = new ByteArrayContent(fileBytes);
            form.Add(content, "file", "file");
            var client = new HttpClient();
            // Add your subscription key to the header Ocp-Apim-Subscription-Key directly
            // Replace "YourSubscriptionKey" with your own subscription key
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "1bd825a94b2f4b74bd50d080da63ca3f");
            // Edit with your desired region for `{region}`
            var response = await client.PostAsync($"https://signature.{region}.cts.speech.microsoft.com/api/v1/Signature/GenerateVoiceSignatureFromFormData", form);
            // A voice signature contains Version, Tag and Data key values from the Signature json structure from the Response body.
            // Voice signature format example: { "Version": <Numeric string or integer value>, "Tag": "string", "Data": "string" }
            var jsonData = await response.Content.ReadAsStringAsync();

            return jsonData;
        }

        static async Task<string> CreateVoiceSignatureByUsingFormDataSteve() {
            // Replace with your own region
            var region = "centralus";
            // Change the name of the wave file to match yours
            byte[] fileBytes = File.ReadAllBytes(@"enrollment_audio_steve.wav");
            var form = new MultipartFormDataContent();
            var content = new ByteArrayContent(fileBytes);
            form.Add(content, "file", "file");
            var client = new HttpClient();
            // Add your subscription key to the header Ocp-Apim-Subscription-Key directly
            // Replace "YourSubscriptionKey" with your own subscription key
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "1bd825a94b2f4b74bd50d080da63ca3f");
            // Edit with your desired region for `{region}`
            var response = await client.PostAsync($"https://signature.{region}.cts.speech.microsoft.com/api/v1/Signature/GenerateVoiceSignatureFromFormData", form);
            // A voice signature contains Version, Tag and Data key values from the Signature json structure from the Response body.
            // Voice signature format example: { "Version": <Numeric string or integer value>, "Tag": "string", "Data": "string" }
            var jsonData = await response.Content.ReadAsStringAsync();

            return jsonData;
        }

        static async Task TranscribirConversacion(SpeechConfig Config, AudioConfig AudioInput) {
            TaskCompletionSource<int> stopTranscription = new TaskCompletionSource<int>();

            // Pick a conversation Id that is a GUID.
            string conversationId = Guid.NewGuid().ToString();

            // Create a Conversation
            using (Conversation conversation = await Conversation.CreateConversationAsync(Config, conversationId).ConfigureAwait(false)) {
                // Create a conversation transcriber using audio stream input
                using (ConversationTranscriber transcriber = new ConversationTranscriber(AudioInput)) {
                    await transcriber.JoinConversationAsync(conversation).ConfigureAwait(false);

                    // Subscribe to events
                    transcriber.Transcribing += (s, e) => {
                        Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text}");
                    };

                    transcriber.Transcribed += (s, e) => {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech) {
                            Console.WriteLine($"TRANSCRIBED: Text={e.Result.Text}, UserID={e.Result.UserId}");
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch) {
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        }
                    };

                    transcriber.Canceled += (s, e) => {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error) {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                            stopTranscription.TrySetResult(0);
                        }
                    };

                    transcriber.SessionStarted += (s, e) => {
                        Console.WriteLine("\nSession started event.");
                    };

                    transcriber.SessionStopped += (s, e) => {
                        Console.WriteLine("\nSession stopped event.");
                        Console.WriteLine("\nStop recognition.");
                        stopTranscription.TrySetResult(0);
                    };

                    // Starts transcribing of the conversation. Uses StopTranscribingAsync() to stop transcribing when all participants leave.
                    await transcriber.StartTranscribingAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopTranscription.Task });

                    // Stop transcribing the conversation.
                    await transcriber.StopTranscribingAsync().ConfigureAwait(false);
                }
            }
        }

        //static async Task CreateVoiceSignatureByUsingBody(string FileName) {
        //    // Replace with your own region
        //    var Region = "centralus";
        //    // Change the name of the wave file to match yours
        //    byte[] FileBytes = File.ReadAllBytes(FileName);
        //    var Content = new ByteArrayContent(FileBytes);

        //    var Client = new HttpClient();
        //    // Add your subscription key to the header Ocp-Apim-Subscription-Key directly
        //    // Replace "YourSubscriptionKey" with your own subscription key
        //    Client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "1bd825a94b2f4b74bd50d080da63ca3f");
        //    // Edit with your desired region for `{region}`
        //    var Response = await Client.PostAsync($"https://signature.{Region}.cts.speech.microsoft.com/api/v1/Signature/GenerateVoiceSignatureFromByteArray", Content).ConfigureAwait(false);
        //    // A voice signature contains Version, Tag and Data key values from the Signature json structure from the Response body.
        //    // Voice signature format example: { "Version": <Numeric string or integer value>, "Tag": "string", "Data": "string" }
        //    var jsonData = await Response.Content.ReadAsStringAsync().ConfigureAwait(false);
        //}
    }

    class JSONData {
        public string Status { get; set; }
        public Signature Signature { get; set; }
    }

    class Signature {
        public string Version { get; set; }
        public string Tag { get; set; }
        public string Data { get; set; }
    }
}
