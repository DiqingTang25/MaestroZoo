using System;

namespace MaestroZoo
{
    [Serializable]
    public class ChartData
    {
        public string songName;
        public int bpm = 120;
        public string difficulty;
        public float leadTime = 2f;
        public ChartNote[] notes;

        /// <summary>Optional tempo change points for songs with variable BPM.</summary>
        public TempoChange[] tempoChanges;

        /// <summary>
        /// Get the BPM at a given song time, accounting for tempo changes.
        /// If no tempoChanges defined, returns the base bpm.
        /// </summary>
        public int GetBpmAtTime(float time)
        {
            if (tempoChanges == null || tempoChanges.Length == 0)
                return bpm;

            int result = bpm;
            for (int i = 0; i < tempoChanges.Length; i++)
            {
                if (tempoChanges[i] != null && tempoChanges[i].time <= time)
                    result = tempoChanges[i].bpm;
            }
            return result;
        }

        /// <summary>First BPM in the chart (considering tempo changes).</summary>
        public int StartBpm
        {
            get
            {
                if (tempoChanges != null && tempoChanges.Length > 0 && tempoChanges[0] != null)
                    return tempoChanges[0].bpm;
                return bpm;
            }
        }

        public float GetEndTime()
        {
            if (notes == null || notes.Length == 0)
                return 0f;

            float endTime = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                if (notes[i] != null && notes[i].time > endTime)
                    endTime = notes[i].time;
            }
            return endTime;
        }
    }

    /// <summary>A tempo change event at a specific song time.</summary>
    [Serializable]
    public class TempoChange
    {
        /// <summary>Time in seconds when this tempo takes effect.</summary>
        public float time;

        /// <summary>Beats per minute starting at this time.</summary>
        public int bpm;
    }

    [Serializable]
    public class ChartNote
    {
        public float time;
        public string gesture;
        public int lane;
        public string animal;

        /// <summary>
        /// Duration in seconds for sustained gestures (long-press / sostenuto).
        /// 0 or negative = instantaneous gesture.
        /// </summary>
        public float duration;

        public GestureType GestureType
        {
            get
            {
                if (Enum.TryParse(gesture, true, out GestureType parsed))
                    return parsed;
                return GestureType.Down;
            }
        }

        /// <summary>True if this note requires a sustained/hold gesture.</summary>
        public bool IsSustained => duration > 0f;
    }
}
