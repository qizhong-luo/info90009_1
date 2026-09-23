using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Sleepet
{
    [Serializable]
    public sealed class InteractionEvent
    {
        public string timestamp;
        public string sessionId;
        public string eventType;
        public string value;
        public string context;
    }

    [Serializable]
    public sealed class SessionRecord
    {
        public string sessionId;
        public string sessionStartTime;
        public List<InteractionEvent> events = new List<InteractionEvent>();
    }

    public sealed class EventLogger
    {
        public SessionRecord Record { get; }
        public string FilePath { get; }
        public string LastError { get; private set; }

        public static string DefaultDirectory => Path.Combine(
#if UNITY_EDITOR || UNITY_STANDALONE
            Path.GetDirectoryName(Application.dataPath),
#else
            Application.persistentDataPath,
#endif
            "SessionLogs");

        public EventLogger(string directory = null)
        {
            Record = new SessionRecord {
                sessionId = Guid.NewGuid().ToString("N"),
                sessionStartTime = DateTime.UtcNow.ToString("O")
            };
            FilePath = Path.Combine(directory ?? DefaultDirectory,
                "sleepet-" + Record.sessionId + ".json");
        }

        public void Log(string type, string value = "", string context = "")
        {
            Record.events.Add(new InteractionEvent {
                timestamp = DateTime.UtcNow.ToString("O"), sessionId = Record.sessionId,
                eventType = type, value = value, context = context
            });
            Save();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, JsonUtility.ToJson(Record, true));
                LastError = null;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                if (LastError == null) Debug.LogWarning("Sleepet log could not be saved: " + ex.Message);
                LastError = ex.Message;
            }
        }
    }
}
