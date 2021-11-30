#region

using System.Collections.Generic;

#endregion

namespace Deplorable_Mountaineer.Console {
    public class HistoryBuffer {
        private readonly List<string> _buffer = new();
        private readonly int _size;
        private int _currentLineNumber = -1;

        public HistoryBuffer(int size = 1000){
            _size = size;
        }

        public int NumUnread => _currentLineNumber - FirstUnreadLineNumber + 1;
        public int FirstInHistory { get; private set; }
        public int NextLineNumber => _currentLineNumber + 1;
        public int FirstUnreadLineNumber { get; private set; }

        public int Append(string line){
            _buffer.Add(line);
            _currentLineNumber++;
            return _currentLineNumber;
        }

        public string Get(){
            if(NumUnread < 1) return null;
            string result = _buffer[FirstUnreadLineNumber - FirstInHistory];
            FirstUnreadLineNumber++;
            ResizeBuffer();
            return result;
        }

        public string CopyFromHistory(int lineNumber){
            if(lineNumber < FirstInHistory || lineNumber >= NextLineNumber) return null;
            return _buffer[lineNumber - FirstInHistory];
        }

        public void Flush(){
            FirstUnreadLineNumber = _currentLineNumber + 1;
            ResizeBuffer();
        }

        private void ResizeBuffer(){
            if(FirstUnreadLineNumber - FirstInHistory > _size){
                _buffer.RemoveRange(0,
                    FirstUnreadLineNumber - FirstInHistory - _size);
                FirstInHistory = FirstUnreadLineNumber - _size;
            }
        }
    }
}