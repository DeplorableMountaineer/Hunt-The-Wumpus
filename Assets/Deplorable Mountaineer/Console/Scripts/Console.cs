#region

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

#endregion

namespace Deplorable_Mountaineer.Console {
    public class Console : MonoBehaviour {
        private static readonly Regex ImmatureRegex = new(
            @"\b(dam(n|mit|it|m)|sh[ai]tt?|(mother)?fuc[hk](er|ing|it)?|he(ll|rion)|b(itch|astard|iden|oomer)|cu(nt|mm?)|(nm)?igg?er|ass|penis|meth|crack|cocaine|fag(g?ot)?|gfy)(s|es|er|ing)?\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [Tooltip("Echoed Input Color")] [SerializeField]
        private Color inputTextColor = Color.green;

        [Tooltip("Response Color")] [SerializeField]
        private Color outputTextColor = new(1, .5f, 0);

        [Tooltip("Highlighted Color")] [SerializeField]
        private Color highlightedTextColor = new(1, 1, .5f);

        [Tooltip("Debug messages; do not appear in production build")] [SerializeField]
        private Color debugTextColor = new(1, 0, .5f);

        [Tooltip("Prevent rich text from being parsed")] [SerializeField]
        private bool preventRichText = true;

        [Tooltip("Prepend this to echoed input in logs")] [SerializeField]
        private string inputLogPrefix = "[INPUT]  ";

        [Tooltip("Prepend this to responses in logs")] [SerializeField]
        private string outputLogPrefix = "[OUTPUT] ";

        [Tooltip("Prepend this to debug messages in logs")] [SerializeField]
        private string debugLogPrefix = "[DEBUG ]  ";

        [Tooltip("Append echoed input to these files")] [SerializeField]
        private string[] defaultInputLogPaths = { "log.txt" };

        [Tooltip("Append responses to these files")] [SerializeField]
        private string[] defaultOutputLogPaths = { "log.txt" };

        [Tooltip("Append debug messages to these files (even in production code)")]
        [SerializeField]
        private string[] defaultDebugLogPaths = { "log.txt" };

        [Tooltip("Clear logs at start instead of appending to existing files")]
        [SerializeField]
        private bool clearLogsInitially = true;

        [Tooltip(
            "Do not remember more than this (for up/down arrow history retrieval in console)")]
        [SerializeField]
        private int inputHistoryBufferSize = 25;

        [Tooltip("Call this whenever something is added to the input buffer")] [SerializeField]
        private UnityEvent onConsoleInput;

        [Tooltip("Display this on awake")] [Multiline] [SerializeField]
        private string welcomeMessage = "Welcome to the World of Tomorrow!";

        private HistoryBuffer _buffer;
        private int _bufferCursor;
        private string _currentText = "";


        /// <summary>
        ///     Text input field component
        /// </summary>
        private TMP_InputField _inputField;

        /// <summary>
        ///     Text display field component
        /// </summary>
        private TMP_Text _textField;

        /// <summary>
        ///     The console instance; only works if there is exactly one!!!
        /// </summary>
        public static Console Instance { get; private set; }

        /// <summary>
        ///     list of paths to echo user input to
        /// </summary>
        public List<string> InputLogPaths { get; } = new();

        /// <summary>
        ///     list of paths to log responses to (when "Say" is called)
        /// </summary>
        public List<string> OutputLogPaths { get; } = new();

        /// <summary>
        ///     list of paths to log debug messages to (when DebugMessage is called)
        /// </summary>
        public List<string> DebugLogPaths { get; } = new();

        /// <summary>
        ///     Number of not-yet-read lines in the input buffer
        /// </summary>
        public int NumUnread => _buffer.NumUnread;

        /// <summary>
        ///     Smallest line number still in the history
        /// </summary>
        public int FirstInHistory => _buffer.FirstInHistory;

        /// <summary>
        ///     Line number to be assigned to the next input entered
        /// </summary>
        public int NextLineNumber => _buffer.NextLineNumber;

        /// <summary>
        ///     Smallest line number not yet read from the input buffer
        /// </summary>
        public int FirstUnreadLineNumber => _buffer.FirstUnreadLineNumber;

        private void Awake(){
            Debug.Log($"Current working directory: {Directory.GetCurrentDirectory()}");
            Instance = this;
            _buffer = new HistoryBuffer(inputHistoryBufferSize);
            _bufferCursor = _buffer.NextLineNumber;
            _inputField = GetComponentInChildren<TMP_InputField>();
            Debug.Assert(_inputField != null, "_inputField != null");
            GameObject display = GameObject.FindGameObjectWithTag("Display");
            Debug.Assert(display != null, "display != null");
            _textField = display.GetComponent<TMP_Text>();
            _textField.color = outputTextColor;
            Debug.Assert(_textField != null, "_textField != null");
            _inputField.ActivateInputField();
            foreach(string path in defaultInputLogPaths) AddInputLogPath(path);
            foreach(string path in defaultOutputLogPaths) AddOutputLogPath(path);
            foreach(string path in defaultDebugLogPaths) AddDebugLogPath(path);
            if(clearLogsInitially){
                foreach(string path in InputLogPaths)
                    if(File.Exists(path))
                        File.Delete(path);

                foreach(string path in OutputLogPaths)
                    if(File.Exists(path))
                        File.Delete(path);

                foreach(string path in DebugLogPaths)
                    if(File.Exists(path))
                        File.Delete(path);
            }
        }

        private void Start(){
            if(!string.IsNullOrWhiteSpace(welcomeMessage)) Say(welcomeMessage);
        }

        private void Update(){
            HandleConsoleTextOverflow();
            HandleConsoleEditing();
        }

        /// <summary>
        ///     Add a path to echo user input to
        /// </summary>
        /// <param name="path"></param>
        public void AddInputLogPath(string path){
            InputLogPaths.Add(path);
        }

        /// <summary>
        ///     Add a path to log responses to
        /// </summary>
        /// <param name="path"></param>
        public void AddOutputLogPath(string path){
            OutputLogPaths.Add(path);
        }

        /// <summary>
        ///     Add a path to log debug messages to
        /// </summary>
        /// <param name="path"></param>
        public void AddDebugLogPath(string path){
            DebugLogPaths.Add(path);
        }

        /// <summary>
        ///     Stop logging user input
        /// </summary>
        public void RemoveInputLogPaths(){
            InputLogPaths.Clear();
        }

        /// <summary>
        ///     Stop logging responses
        /// </summary>
        public void RemoveOutputLogPaths(){
            OutputLogPaths.Clear();
        }

        /// <summary>
        ///     Stop logging debug messages
        /// </summary>
        public void RemoveDebugLogPaths(){
            DebugLogPaths.Clear();
        }

        /// <summary>
        ///     Display command history on the console
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void ShowHistory(int start = 0, int end = int.MaxValue){
            int s = Mathf.Max(start, _buffer.FirstInHistory);
            int e = Mathf.Min(end, _buffer.NextLineNumber - 1);
            if(e < 0) return;
            for(int i = s; i <= e; i++) Say($"[{i}] {_buffer.CopyFromHistory(i)}");
        }

        public void Say(string text, bool highlighted, bool debug){
            if(debug) DebugMessage(text);
            else if(highlighted) SayHighlighted(text);
            else Say(text);
        }

        /// <summary>
        ///     Display a response on the console, and log it to the output log files if any.
        /// </summary>
        /// <param name="objects">strings or other objects to log.</param>
        public void Say(params object[] objects){
            string message = ShowText(Instance._textField, outputTextColor, objects);
            if(message != null)
                foreach(string path in OutputLogPaths)
                    File.AppendAllText(path, outputLogPrefix + message + "\n");
        }

        /// <summary>
        ///     Display a response on the console, and log it to the output log files if any.
        /// </summary>
        /// <param name="objects">strings or other objects to log.</param>
        public void SayHighlighted(params object[] objects){
            string message = ShowText(Instance._textField, highlightedTextColor, objects);
            if(message != null)
                foreach(string path in OutputLogPaths)
                    File.AppendAllText(path, outputLogPrefix + message + "\n");
        }

        /// <summary>
        ///     Append a line to the input buffer
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public int Append(string line){
            return _buffer.Append(line);
        }

        /// <summary>
        ///     Retrieve (and consider read) a line from the input buffer
        /// </summary>
        /// <returns></returns>
        public string Get(){
            return _buffer.Get();
        }

        /// <summary>
        ///     Get, without removing or marking read, a line from the
        ///     history, if it exists.  Return null if it doesn't.
        /// </summary>
        /// <returns></returns>
        public string CopyFromHistory(int lineNumber){
            return _buffer.CopyFromHistory(lineNumber);
        }

        /// <summary>
        ///     Discard unread lines in buffer.  They will be in history, just not processed.
        /// </summary>
        public void Flush(){
            _buffer.Flush();
        }

        /// <summary>
        ///     In a debug build or in the editor, display a debug message in the console.
        ///     In any case, log a debug message to the debug paths, if any.
        /// </summary>
        /// <param name="objects">strings or other objects to log.</param>
        public void DebugMessage(params object[] objects){
#if DEBUG
            string message = ShowText(Instance._textField, debugTextColor, objects);
#else
            string message = null;
            if(objects != null && objects.Length > 0){
                message = string.Join(" ", objects).ToString();
                message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
            }
#endif
            if(message == null) return;
            foreach(string path in DebugLogPaths)
                File.AppendAllText(path, debugLogPrefix + message + "\n");
        }


        /// <summary>
        ///     Process up and down arrow keys to retrieve lines from history
        /// </summary>
        private void HandleConsoleEditing(){
            if(Input.GetKeyDown(KeyCode.UpArrow)){
                if(_bufferCursor == _buffer.NextLineNumber) _currentText = _inputField.text;
                _bufferCursor--;
                if(_bufferCursor < _buffer.FirstInHistory){
                    _bufferCursor = _buffer.FirstInHistory;
                    _inputField.text = _currentText;
                }

                _inputField.text = _buffer.CopyFromHistory(_bufferCursor);
                _inputField.caretPosition = _inputField.text.Length;
                return;
            }

            if(!Input.GetKeyDown(KeyCode.DownArrow)) return;
            if(_bufferCursor == _buffer.NextLineNumber) return;
            _bufferCursor++;
            _inputField.text = _bufferCursor == _buffer.NextLineNumber
                ? _currentText
                : _buffer.CopyFromHistory(_bufferCursor);
            _inputField.caretPosition = _inputField.text.Length;
        }

        /// <summary>
        ///     Scroll the console as necessary
        /// </summary>
        private void HandleConsoleTextOverflow(){
            if(!_textField.isTextOverflowing) return;
            int firstNewLine = _textField.text.IndexOf('\n');
            if(firstNewLine < 0){
                int l = Mathf.Min(80, _textField.text.Length);
                _textField.text = _textField.text.Substring(l);
                return;
            }

            _textField.text = _textField.text.Substring(firstNewLine + 1);
        }

        /// <summary>
        ///     Display the text on the console
        /// </summary>
        /// <param name="textfield"></param>
        /// <param name="color"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        private string ShowText(TMP_Text textfield, Color color, params object[] objects){
            if(objects == null || objects.Length <= 0) return null;
            string message = string.Join(" ", objects);
            if(string.IsNullOrWhiteSpace(message)) return null;
            string t = message;
            if(preventRichText){
                t = message.Replace('<', '«');
                t = t.Replace('>', '»');
            }

            string prefix = "";
            string suffix = "";
            while(t.StartsWith("\n") || t.StartsWith("\r")){
                prefix += t[0];
                t = t.Substring(1);
            }

            while(t.EndsWith("\n") || t.EndsWith("\r")){
                prefix += t[t.Length - 1];
                t = t.Substring(0, t.Length - 1);
            }


            textfield.text += $"{prefix}<color=#{ToHexString(color)}>{t}</color>{suffix}\n";
            return message.Trim();
        }

        /// <summary>
        ///     Echo text intended as user input to the console
        /// </summary>
        /// <param name="text"></param>
        private void EchoInput(string text){
            string message = ShowText(Instance._textField, inputTextColor, text);
            if(message != null)
                foreach(string path in InputLogPaths)
                    File.AppendAllText(path, inputLogPrefix + message + "\n");
        }

        private static string ToHexString(Color color){
            return
                ((byte)(color.r*255)).ToString("X2") +
                ((byte)(color.g*255)).ToString("X2") +
                ((byte)(color.b*255)).ToString("X2") +
                ((byte)(color.a*255)).ToString("X2");
        }

        /// <summary>
        ///     Input field's On Edit inspector property should call this.
        /// </summary>
        /// <param name="text"></param>
        public void OnEdit(string text){
            if(HasSwear(text, out _)) text = "Come on, grow up already!";

            if(text.Length > 0){
                EchoInput($"[{_buffer.NextLineNumber}] " + text);
                _buffer.Append(text.Trim());
                onConsoleInput?.Invoke();
            }

            _inputField.text = "";
            _bufferCursor = _buffer.NextLineNumber;
            _inputField.ActivateInputField();
        }

        private static bool HasSwear(string text, out Match match){
            match = ImmatureRegex.Match(text);
            return match.Success;
        }
    }
}