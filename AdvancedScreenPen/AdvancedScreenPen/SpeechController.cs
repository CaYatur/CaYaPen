using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using SynthesisStateChangedEventArgs = System.Speech.Synthesis.StateChangedEventArgs;

namespace AdvancedScreenPen
{
    public class SpeechController : IDisposable
    {
        private SpeechRecognitionEngine recognitionEngine;
        private SpeechSynthesizer synthesizer;
        private bool isListening = false;
        private bool isEnabled = true;
        private readonly Dictionary<string, VoiceCommand> voiceCommands;
        private readonly List<string> customCommands;

        // Settings
        public string Language { get; set; } = "tr-TR";
        public float ConfidenceThreshold { get; set; } = 0.7f;
        public bool EnableFeedback { get; set; } = true;
        public bool ContinuousListening { get; set; } = true;
        public int SpeechRate { get; set; } = 0;
        public int Volume { get; set; } = 80;
        public string PreferredVoice { get; set; } = "Default";

        // Advanced features
        public bool EnableLearning { get; set; } = true;
        public bool EnableContextAwareness { get; set; } = true;
        public bool EnableMultiLanguage { get; set; } = false;
        public int CommandTimeout { get; set; } = 5000;

        // Events
        public event EventHandler<VoiceCommandEventArgs> CommandRecognized;
        public event EventHandler<SpeechRecognitionEventArgs> SpeechRecognized;
        public event EventHandler<SpeechErrorEventArgs> SpeechError;
        public event EventHandler<VoiceStateChangedEventArgs> StateChanged;

        // Properties
        public bool IsListening => isListening;
        public bool IsEnabled => isEnabled;
        public IReadOnlyDictionary<string, VoiceCommand> VoiceCommands => voiceCommands;
        public float CurrentConfidence { get; private set; }
        public string LastRecognizedText { get; private set; }
        public DateTime LastCommandTime { get; private set; }

        public SpeechController()
        {
            voiceCommands = new Dictionary<string, VoiceCommand>();
            customCommands = new List<string>();

            InitializeSpeechEngine();
            LoadDefaultCommands();
        }

        public bool StartListening()
        {
            if (!isEnabled || isListening)
                return false;

            try
            {
                if (recognitionEngine != null)
                {
                    if (ContinuousListening)
                    {
                        recognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
                    }
                    else
                    {
                        recognitionEngine.RecognizeAsync(RecognizeMode.Single);
                    }

                    isListening = true;
                    OnStateChanged("Listening started", VoiceState.Listening);

                    if (EnableFeedback)
                    {
                        Speak("Ses kontrolü etkinleştirildi");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                OnSpeechError($"Failed to start listening: {ex.Message}");
            }

            return false;
        }

        public bool StopListening()
        {
            if (!isListening)
                return false;

            try
            {
                if (recognitionEngine != null)
                {
                    recognitionEngine.RecognizeAsyncStop();
                    isListening = false;
                    OnStateChanged("Listening stopped", VoiceState.Stopped);

                    if (EnableFeedback)
                    {
                        Speak("Ses kontrolü durduruldu");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                OnSpeechError($"Failed to stop listening: {ex.Message}");
            }

            return false;
        }

        public void ToggleListening()
        {
            if (isListening)
            {
                StopListening();
            }
            else
            {
                StartListening();
            }
        }

        public void AddVoiceCommand(string name, string[] phrases, string description, VoiceCommandHandler handler)
        {
            var command = new VoiceCommand
            {
                Name = name,
                Phrases = phrases,
                Description = description,
                Handler = handler,
                IsEnabled = true,
                AddedTime = DateTime.Now
            };

            voiceCommands[name] = command;
            UpdateGrammar();
        }

        public void RemoveVoiceCommand(string name)
        {
            if (voiceCommands.ContainsKey(name))
            {
                voiceCommands.Remove(name);
                UpdateGrammar();
            }
        }

        public void EnableCommand(string name, bool enabled = true)
        {
            if (voiceCommands.ContainsKey(name))
            {
                voiceCommands[name].IsEnabled = enabled;
                UpdateGrammar();
            }
        }

        public async Task SpeakAsync(string text)
        {
            if (!isEnabled || synthesizer == null || string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                synthesizer.Rate = SpeechRate;
                synthesizer.Volume = Volume;

                var completion = new TaskCompletionSource<bool>();

                void OnSpeakCompleted(object sender, SpeakCompletedEventArgs e)
                {
                    synthesizer.SpeakCompleted -= OnSpeakCompleted;
                    completion.SetResult(!e.Cancelled && e.Error == null);
                }

                synthesizer.SpeakCompleted += OnSpeakCompleted;
                synthesizer.SpeakAsync(text);

                await completion.Task;
            }
            catch (Exception ex)
            {
                OnSpeechError($"Speech synthesis failed: {ex.Message}");
            }
        }

        public void Speak(string text)
        {
            if (!isEnabled || synthesizer == null || string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                synthesizer.Rate = SpeechRate;
                synthesizer.Volume = Volume;
                synthesizer.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                OnSpeechError($"Speech synthesis failed: {ex.Message}");
            }
        }

        public void SetLanguage(string languageCode)
        {
            if (Language == languageCode)
                return;

            try
            {
                Language = languageCode;

                // Reinitialize speech engine with new language
                DisposeSpeechEngine();
                InitializeSpeechEngine();
                LoadDefaultCommands();

                OnStateChanged($"Language changed to {languageCode}", VoiceState.LanguageChanged);
            }
            catch (Exception ex)
            {
                OnSpeechError($"Failed to change language: {ex.Message}");
            }
        }

        public void SetVoice(string voiceName)
        {
            if (synthesizer == null)
                return;

            try
            {
                if (voiceName != "Default")
                {
                    synthesizer.SelectVoice(voiceName);
                }
                else
                {
                    synthesizer.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet);
                }

                PreferredVoice = voiceName;
                OnStateChanged($"Voice changed to {voiceName}", VoiceState.VoiceChanged);
            }
            catch (Exception ex)
            {
                OnSpeechError($"Failed to set voice: {ex.Message}");
            }
        }

        public List<string> GetAvailableVoices()
        {
            var voices = new List<string> { "Default" };

            if (synthesizer != null)
            {
                foreach (InstalledVoice voice in synthesizer.GetInstalledVoices())
                {
                    if (voice.Enabled && voice.VoiceInfo.Culture.Name == Language)
                    {
                        voices.Add(voice.VoiceInfo.Name);
                    }
                }
            }

            return voices;
        }

        public void TrainCommand(string commandName, string newPhrase)
        {
            if (!EnableLearning || !voiceCommands.ContainsKey(commandName))
                return;

            var command = voiceCommands[commandName];
            var phrasesList = command.Phrases.ToList();

            if (!phrasesList.Contains(newPhrase, StringComparer.OrdinalIgnoreCase))
            {
                phrasesList.Add(newPhrase);
                command.Phrases = phrasesList.ToArray();
                UpdateGrammar();

                OnStateChanged($"Command '{commandName}' trained with new phrase", VoiceState.CommandTrained);
            }
        }

        public VoiceCommandStatistics GetStatistics()
        {
            var stats = new VoiceCommandStatistics
            {
                TotalCommands = voiceCommands.Count,
                EnabledCommands = voiceCommands.Count(c => c.Value.IsEnabled),
                CustomCommands = customCommands.Count,
                LastCommandTime = LastCommandTime,
                AverageConfidence = CurrentConfidence,
                Language = Language,
                IsListening = isListening
            };

            return stats;
        }

        private void InitializeSpeechEngine()
        {
            try
            {
                // Initialize recognizer
                recognitionEngine = new SpeechRecognitionEngine(new CultureInfo(Language));
                recognitionEngine.SpeechRecognized += RecognitionEngine_SpeechRecognized;
                recognitionEngine.SpeechRecognitionRejected += RecognitionEngine_SpeechRecognitionRejected;
                recognitionEngine.RecognizeCompleted += RecognitionEngine_RecognizeCompleted;
                recognitionEngine.AudioLevelUpdated += RecognitionEngine_AudioLevelUpdated;
                recognitionEngine.SetInputToDefaultAudioDevice();

                // Initialize synthesizer
                synthesizer = new SpeechSynthesizer();
                synthesizer.SetOutputToDefaultAudioDevice();
                synthesizer.StateChanged += Synthesizer_StateChanged;
                synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;

                // Set voice if specified
                if (PreferredVoice != "Default")
                {
                    SetVoice(PreferredVoice);
                }

                OnStateChanged("Speech engine initialized", VoiceState.Initialized);
            }
            catch (Exception ex)
            {
                OnSpeechError($"Failed to initialize speech engines: {ex.Message}");
            }
        }

        private void DisposeSpeechEngine()
        {
            try
            {
                if (isListening)
                {
                    StopListening();
                }

                if (recognitionEngine != null)
                {
                    recognitionEngine.SpeechRecognized -= RecognitionEngine_SpeechRecognized;
                    recognitionEngine.SpeechRecognitionRejected -= RecognitionEngine_SpeechRecognitionRejected;
                    recognitionEngine.RecognizeCompleted -= RecognitionEngine_RecognizeCompleted;
                    recognitionEngine.AudioLevelUpdated -= RecognitionEngine_AudioLevelUpdated;
                    recognitionEngine.Dispose();
                    recognitionEngine = null;
                }

                if (synthesizer != null)
                {
                    synthesizer.StateChanged -= Synthesizer_StateChanged;
                    synthesizer.SpeakCompleted -= Synthesizer_SpeakCompleted;
                    synthesizer.Dispose();
                    synthesizer = null;
                }
            }
            catch (Exception ex)
            {
                OnSpeechError($"Error disposing speech engine: {ex.Message}");
            }
        }

        private void LoadDefaultCommands()
        {
            if (Language.StartsWith("tr"))
            {
                LoadTurkishCommands();
            }
            else
            {
                LoadEnglishCommands();
            }

            UpdateGrammar();
        }

        private void LoadTurkishCommands()
        {
            // Temel komutlar
            AddVoiceCommand("temizle", new[] { "temizle", "sil", "ekranı temizle" }, "Ekranı temizler",
                args => args.Parameters["action"] = "clear");

            AddVoiceCommand("geri_al", new[] { "geri al", "undo", "önceki" }, "Son işlemi geri alır",
                args => args.Parameters["action"] = "undo");

            AddVoiceCommand("yinele", new[] { "yinele", "redo", "ileri" }, "İşlemi yineler",
                args => args.Parameters["action"] = "redo");

            // Araç seçimi
            AddVoiceCommand("kalem", new[] { "kalem", "pen", "kalem seç" }, "Kalem aracını seçer",
                args => { args.Parameters["action"] = "select_tool"; args.Parameters["tool"] = "pen"; });

            AddVoiceCommand("firca", new[] { "fırça", "brush", "fırça seç" }, "Fırça aracını seçer",
                args => { args.Parameters["action"] = "select_tool"; args.Parameters["tool"] = "brush"; });

            AddVoiceCommand("silgi", new[] { "silgi", "eraser", "silgi seç" }, "Silgi aracını seçer",
                args => { args.Parameters["action"] = "select_tool"; args.Parameters["tool"] = "eraser"; });

            AddVoiceCommand("fosforlu", new[] { "fosforlu kalem", "highlighter", "işaretleyici" }, "Fosforlu kalem seçer",
                args => { args.Parameters["action"] = "select_tool"; args.Parameters["tool"] = "highlighter"; });

            // Renk seçimi
            AddVoiceCommand("kirmizi", new[] { "kırmızı", "red", "kırmızı renk" }, "Kırmızı renk seçer",
                args => { args.Parameters["action"] = "select_color"; args.Parameters["color"] = "Red"; });

            AddVoiceCommand("mavi", new[] { "mavi", "blue", "mavi renk" }, "Mavi renk seçer",
                args => { args.Parameters["action"] = "select_color"; args.Parameters["color"] = "Blue"; });

            AddVoiceCommand("yesil", new[] { "yeşil", "green", "yeşil renk" }, "Yeşil renk seçer",
                args => { args.Parameters["action"] = "select_color"; args.Parameters["color"] = "Green"; });

            AddVoiceCommand("sari", new[] { "sarı", "yellow", "sarı renk" }, "Sarı renk seçer",
                args => { args.Parameters["action"] = "select_color"; args.Parameters["color"] = "Yellow"; });

            AddVoiceCommand("siyah", new[] { "siyah", "black", "siyah renk" }, "Siyah renk seçer",
                args => { args.Parameters["action"] = "select_color"; args.Parameters["color"] = "Black"; });

            AddVoiceCommand("beyaz", new[] { "beyaz", "white", "beyaz renk" }, "Beyaz renk seçer",
                args => { args.Parameters["action"] = "select_color"; args.Parameters["color"] = "White"; });

            // Boyut ayarları
            AddVoiceCommand("kucuk", new[] { "küçük", "small", "ince", "küçük boyut" }, "Küçük boyut seçer",
                args => { args.Parameters["action"] = "set_size"; args.Parameters["size"] = "small"; });

            AddVoiceCommand("orta", new[] { "orta", "medium", "normal", "orta boyut" }, "Orta boyut seçer",
                args => { args.Parameters["action"] = "set_size"; args.Parameters["size"] = "medium"; });

            AddVoiceCommand("buyuk", new[] { "büyük", "large", "kalın", "büyük boyut" }, "Büyük boyut seçer",
                args => { args.Parameters["action"] = "set_size"; args.Parameters["size"] = "large"; });

            // Dosya işlemleri
            AddVoiceCommand("kaydet", new[] { "kaydet", "save", "dosyayı kaydet" }, "Dosyayı kaydeder",
                args => args.Parameters["action"] = "save");

            AddVoiceCommand("ac", new[] { "aç", "open", "dosya aç" }, "Dosya açar",
                args => args.Parameters["action"] = "open");

            AddVoiceCommand("yeni", new[] { "yeni", "new", "yeni dosya" }, "Yeni dosya oluşturur",
                args => args.Parameters["action"] = "new");

            // Uygulama kontrolü
            AddVoiceCommand("cikis", new[] { "çıkış", "exit", "kapat", "uygulamayı kapat" }, "Uygulamayı kapatır",
                args => args.Parameters["action"] = "exit");

            AddVoiceCommand("tam_ekran", new[] { "tam ekran", "fullscreen", "büyüt" }, "Tam ekran modu",
                args => args.Parameters["action"] = "fullscreen");

            AddVoiceCommand("minimize", new[] { "küçült", "minimize", "gizle" }, "Uygulamayı küçültür",
                args => args.Parameters["action"] = "minimize");

            // Gelişmiş komutlar
            AddVoiceCommand("efekt", new[] { "efekt", "effect", "efekt uygula" }, "Efekt menüsünü açar",
                args => args.Parameters["action"] = "effects");

            AddVoiceCommand("katman", new[] { "katman", "layer", "yeni katman" }, "Yeni katman oluşturur",
                args => args.Parameters["action"] = "new_layer");

            AddVoiceCommand("zoom_in", new[] { "yakınlaştır", "zoom in", "büyüt" }, "Yakınlaştırır",
                args => args.Parameters["action"] = "zoom_in");

            AddVoiceCommand("zoom_out", new[] { "uzaklaştır", "zoom out", "küçült" }, "Uzaklaştırır",
                args => args.Parameters["action"] = "zoom_out");
        }

        private void LoadEnglishCommands()
        {
            // Basic commands
            AddVoiceCommand("clear", new[] { "clear", "clear screen", "delete all" }, "Clears the screen",
                args => args.Parameters["action"] = "clear");

            AddVoiceCommand("undo", new[] { "undo", "go back", "previous" }, "Undoes last action",
                args => args.Parameters["action"] = "undo");

            AddVoiceCommand("redo", new[] { "redo", "forward", "repeat" }, "Redoes action",
                args => args.Parameters["action"] = "redo");

            // Tool selection
            AddVoiceCommand("pen", new[] { "pen", "select pen", "pencil" }, "Selects pen tool",
                args => { args.Parameters["action"] = "select_tool"; args.Parameters["tool"] = "pen"; });

            AddVoiceCommand("brush", new[] { "brush", "select brush", "paint brush" }, "Selects brush tool",
                args => { args.Parameters["action"] = "select_tool"; args.Parameters["tool"] = "brush"; });

            AddVoiceCommand("eraser", new[] { "eraser", "select eraser", "delete" }, "Selects eraser tool",
                args => { args.Parameters["action"] = "select_tool"; args.Parameters["tool"] = "eraser"; });

            AddVoiceCommand("highlighter", new[] { "highlighter", "highlight", "marker" }, "Selects highlighter",
                args => { args.Parameters["action"] = "select_tool"; args.Parameters["tool"] = "highlighter"; });

            // Color selection
            AddVoiceCommand("red", new[] { "red", "red color", "select red" }, "Selects red color",
                args => { args.Parameters["action"] = "select_color"; args.Parameters["color"] = "Red"; });

            AddVoiceCommand("blue", new[] { "blue", "blue color", "select blue" }, "Selects blue color",
                args => { args.Parameters["action"] = "select_color"; args.Parameters["color"] = "Blue"; });

            AddVoiceCommand("green", new[] { "green", "green color", "select green" }, "Selects green color",
                args => { args.Parameters["action"] = "select_color"; args.Parameters["color"] = "Green"; });

            AddVoiceCommand("yellow", new[] { "yellow", "yellow color", "select yellow" }, "Selects yellow color",
                args => { args.Parameters["action"] = "select_color"; args.Parameters["color"] = "Yellow"; });

            AddVoiceCommand("black", new[] { "black", "black color", "select black" }, "Selects black color",
                args => { args.Parameters["action"] = "select_color"; args.Parameters["color"] = "Black"; });

            AddVoiceCommand("white", new[] { "white", "white color", "select white" }, "Selects white color",
                args => { args.Parameters["action"] = "select_color"; args.Parameters["color"] = "White"; });

            // Size settings
            AddVoiceCommand("small", new[] { "small", "small size", "thin" }, "Selects small size",
                args => { args.Parameters["action"] = "set_size"; args.Parameters["size"] = "small"; });

            AddVoiceCommand("medium", new[] { "medium", "medium size", "normal" }, "Selects medium size",
                args => { args.Parameters["action"] = "set_size"; args.Parameters["size"] = "medium"; });

            AddVoiceCommand("large", new[] { "large", "large size", "thick", "big" }, "Selects large size",
                args => { args.Parameters["action"] = "set_size"; args.Parameters["size"] = "large"; });

            // File operations
            AddVoiceCommand("save", new[] { "save", "save file" }, "Saves the file",
                args => args.Parameters["action"] = "save");

            AddVoiceCommand("open", new[] { "open", "open file" }, "Opens a file",
                args => args.Parameters["action"] = "open");

            AddVoiceCommand("new", new[] { "new", "new file", "create new" }, "Creates new file",
                args => args.Parameters["action"] = "new");

            // Application control
            AddVoiceCommand("exit", new[] { "exit", "close", "quit" }, "Closes application",
                args => args.Parameters["action"] = "exit");

            AddVoiceCommand("fullscreen", new[] { "fullscreen", "full screen", "maximize" }, "Fullscreen mode",
                args => args.Parameters["action"] = "fullscreen");

            AddVoiceCommand("minimize", new[] { "minimize", "hide" }, "Minimizes application",
                args => args.Parameters["action"] = "minimize");
        }

        private void UpdateGrammar()
        {
            if (recognitionEngine == null)
                return;

            try
            {
                recognitionEngine.UnloadAllGrammars();

                var grammarBuilder = new GrammarBuilder();
                var choices = new Choices();

                foreach (var command in voiceCommands.Values.Where(c => c.IsEnabled))
                {
                    foreach (var phrase in command.Phrases)
                    {
                        choices.Add(phrase);
                    }
                }

                if (choices.ToGrammarBuilder().DebugShowPhrases.Length > 0)
                {
                    grammarBuilder.Append(choices);
                    var grammar = new Grammar(grammarBuilder);
                    recognitionEngine.LoadGrammar(grammar);
                }

                OnStateChanged("Grammar updated", VoiceState.GrammarUpdated);
            }
            catch (Exception ex)
            {
                OnSpeechError($"Failed to update grammar: {ex.Message}");
            }
        }

        // Event handlers
        private void RecognitionEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            try
            {
                CurrentConfidence = e.Result.Confidence;
                LastRecognizedText = e.Result.Text;
                LastCommandTime = DateTime.Now;

                OnSpeechRecognized(e.Result.Text, e.Result.Confidence);

                if (e.Result.Confidence >= ConfidenceThreshold)
                {
                    ProcessVoiceCommand(e.Result.Text, e.Result.Confidence);
                }
                else
                {
                    OnSpeechError($"Low confidence: {e.Result.Confidence:F2} < {ConfidenceThreshold:F2}");
                }
            }
            catch (Exception ex)
            {
                OnSpeechError($"Error processing recognized speech: {ex.Message}");
            }
        }

        private void RecognitionEngine_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            OnSpeechError($"Speech recognition rejected: {e.Result?.Text ?? "Unknown"}");
        }

        private void RecognitionEngine_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (ContinuousListening && isListening && !e.Cancelled)
            {
                // Restart recognition for continuous listening
                try
                {
                    recognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
                }
                catch (Exception ex)
                {
                    OnSpeechError($"Failed to restart recognition: {ex.Message}");
                }
            }
        }

        private void RecognitionEngine_AudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {
            // Can be used for audio level monitoring
        }

        private void Synthesizer_StateChanged(object sender, SynthesisStateChangedEventArgs e)
        {
            OnStateChanged($"Synthesizer state: {e.State}", VoiceState.SynthesizerStateChanged);
        }

        private void Synthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                OnSpeechError($"Speech synthesis error: {e.Error.Message}");
            }
        }

        private void ProcessVoiceCommand(string recognizedText, float confidence)
        {
            var command = FindMatchingCommand(recognizedText);
            if (command != null)
            {
                var args = new VoiceCommandEventArgs
                {
                    CommandName = command.Name,
                    RecognizedText = recognizedText,
                    Confidence = confidence,
                    Parameters = new Dictionary<string, object>(),
                    Timestamp = DateTime.Now
                };

                try
                {
                    command.Handler?.Invoke(args);
                    OnCommandRecognized(args);

                    if (EnableFeedback)
                    {
                        Speak("Tamam");
                    }
                }
                catch (Exception ex)
                {
                    OnSpeechError($"Error executing command '{command.Name}': {ex.Message}");
                }
            }
            else
            {
                OnSpeechError($"No matching command found for: {recognizedText}");
            }
        }

        private VoiceCommand FindMatchingCommand(string text)
        {
            text = text.ToLowerInvariant().Trim();

            foreach (var command in voiceCommands.Values.Where(c => c.IsEnabled))
            {
                foreach (var phrase in command.Phrases)
                {
                    if (phrase.ToLowerInvariant().Trim() == text)
                    {
                        return command;
                    }
                }
            }

            // Fuzzy matching for partial matches
            if (EnableContextAwareness)
            {
                foreach (var command in voiceCommands.Values.Where(c => c.IsEnabled))
                {
                    foreach (var phrase in command.Phrases)
                    {
                        if (text.Contains(phrase.ToLowerInvariant()) || phrase.ToLowerInvariant().Contains(text))
                        {
                            return command;
                        }
                    }
                }
            }

            return null;
        }

        // Event invokers
        protected virtual void OnCommandRecognized(VoiceCommandEventArgs args)
        {
            CommandRecognized?.Invoke(this, args);
        }

        protected virtual void OnSpeechRecognized(string text, float confidence)
        {
            SpeechRecognized?.Invoke(this, new SpeechRecognitionEventArgs
            {
                RecognizedText = text,
                Confidence = confidence,
                Timestamp = DateTime.Now
            });
        }

        protected virtual void OnSpeechError(string message)
        {
            SpeechError?.Invoke(this, new SpeechErrorEventArgs
            {
                Message = message,
                Timestamp = DateTime.Now
            });
        }

        protected virtual void OnStateChanged(string message, VoiceState state)
        {
            StateChanged?.Invoke(this, new VoiceStateChangedEventArgs
            {
                Message = message,
                State = state,
                Timestamp = DateTime.Now
            });
        }

        public void Dispose()
        {
            DisposeSpeechEngine();
        }
    }

    // Supporting classes and enums
    public enum VoiceState
    {
        Initialized,
        Listening,
        Stopped,
        Processing,
        Speaking,
        Error,
        LanguageChanged,
        VoiceChanged,
        CommandTrained,
        GrammarUpdated,
        SynthesizerStateChanged
    }

    public class VoiceCommand
    {
        public string Name { get; set; }
        public string[] Phrases { get; set; }
        public string Description { get; set; }
        public VoiceCommandHandler Handler { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime AddedTime { get; set; }
        public int UsageCount { get; set; }
        public float AverageConfidence { get; set; }
    }

    public class VoiceCommandStatistics
    {
        public int TotalCommands { get; set; }
        public int EnabledCommands { get; set; }
        public int CustomCommands { get; set; }
        public DateTime LastCommandTime { get; set; }
        public float AverageConfidence { get; set; }
        public string Language { get; set; }
        public bool IsListening { get; set; }
    }

    // Event argument classes
    public class VoiceCommandEventArgs : EventArgs
    {
        public string CommandName { get; set; }
        public string RecognizedText { get; set; }
        public float Confidence { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SpeechRecognitionEventArgs : EventArgs
    {
        public string RecognizedText { get; set; }
        public float Confidence { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SpeechErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class VoiceStateChangedEventArgs : EventArgs
    {
        public string Message { get; set; }
        public VoiceState State { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Delegate for voice command handlers
    public delegate void VoiceCommandHandler(VoiceCommandEventArgs args);
}