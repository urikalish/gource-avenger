using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mercury.TD.Client.Ota.Api;
using Mercury.TD.Client.Ota.Api.Extensions;

namespace Avenger
{
    public enum LogEntryType
    {
        A,
        M,
        D
    }
    
    public class LogFileEntry : IComparable
    {
        public long Time { get; set; }
        public string TextLine { get; set; }
        public int CompareTo(object obj)
        {
            return obj as LogFileEntry == null ? 0 : Time.CompareTo((obj as LogFileEntry).Time);
        }
    }

    public class HistoryRecComparer : IComparer<IHistoryEntry>
    {
        public int Compare(IHistoryEntry x, IHistoryEntry y)
        {
            return x.ChangeDate.CompareTo(y.ChangeDate);
        }
    }

    public class LogFileHelper
    {
        public List<string> PathFields = new List<string>();
        public List<string> OrderedFieldsValuesForColoring = new List<string>();
        public Dictionary<string, string> MapFieldValueToColor = new Dictionary<string, string>();

        private IBaseEntity _entity;
        private IHistoryEntry _historyEntry;
        private string _field;
        private string _value;
        private bool _isPath;
        private bool _isColor;
        private Dictionary<string, string> _lastValues = new Dictionary<string, string>();
        private List<LogFileEntry> _logFileEntries = new List<LogFileEntry>();
        private StringBuilder _sb = new StringBuilder();
        private char _entryType;
        private string _entryColor;
        
        public void HandleOneEntity(IBaseEntity entity)
        {
            if (entity as ISupportHistoryEntity == null) return;
            _entity = entity;

            _lastValues.Clear();
            var e = entity as ISupportHistoryEntity;
            var hrs = new List<IHistoryEntry>();
            foreach (var hr in e.GetHistoryRecords(e.NewHistoryFilter()))
            {
                hrs.Add(hr);
            }
            hrs.Sort(new HistoryRecComparer());
            foreach (var hr in hrs)
            {
                _historyEntry = hr;
                HandleOneHistoryRecord();
            }
        }

        private void HandleOneHistoryRecord()
        {
            _field = _historyEntry.ChangedField.Appearance.Label;
            _value = _historyEntry.ChangedTo.ToString();
            _isPath = PathFields.Contains(_field);
            _isColor = MapFieldValueToColor.ContainsKey(_field + "|" + _value);
            if (!_isPath && !_isColor)
                return;

            var entirePathExistsBefore = PathFields.TrueForAll(pathField => _lastValues.ContainsKey(pathField));
            if (entirePathExistsBefore && _isPath)
            {
                _entryType = 'D';
                AddEntry();
            }
            _lastValues[_field] = _value;
            var entirePathExistsAfter = PathFields.TrueForAll(pathField => _lastValues.ContainsKey(pathField));
            if (!entirePathExistsAfter)
                return;

            var fieldvalue = OrderedFieldsValuesForColoring.Find(fv => _lastValues.ContainsKey(fv.Split('|')[0]) && _lastValues[fv.Split('|')[0]] == fv.Split('|')[1]);
            if (string.IsNullOrEmpty(fieldvalue))
                return;

            _entryColor = MapFieldValueToColor[fieldvalue];
            _entryType = 'M';
            AddEntry();
        }

        private void AddEntry()
        {
            var time = (long)((_historyEntry.ChangeDate - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds);
            var entry = new LogFileEntry { Time = time };
            _sb.Clear();
            for (var p = 0; p < PathFields.Count - 1; p++)
            {
                if (p > 0)
                {
                    _sb.Append("/");
                }
                _sb.Append(_lastValues[PathFields[p]].Replace("|", "_").Replace("/", "_"));
            }
            _sb.Append(string.Format("/{0}.{1}", _entity.Id, _lastValues[PathFields[PathFields.Count - 1]].Replace("|", "_").Replace("/", "_")));
            switch (_entryType)
            {
                case 'M':
                case 'A':
                    entry.TextLine = string.Format("{0}|{1}|{2}|{3}|{4}", entry.Time, _historyEntry.ChangedBy, _entryType, _sb, _entryColor);
                    break;
                case 'D':
                    entry.TextLine = string.Format("{0}|{1}|{2}|{3}", entry.Time, _historyEntry.ChangedBy, "D", _sb);
                    break;
            }
            _logFileEntries.Add(entry);    
        }

        public void SortLogFileEntries()
        {
            _logFileEntries.Sort();
        }

        public void SaveEntriesToLogFile(string logFileName)
        {
            foreach (var logFileEntry in _logFileEntries)
            {
                File.AppendAllText(logFileName, logFileEntry.TextLine);
                File.AppendAllText(logFileName, Environment.NewLine);
            }
        }

    }
}
