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

        public float GetEndTime()
        {
            if (notes == null || notes.Length == 0)
            {
                return 0f;
            }

            float endTime = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                if (notes[i] != null && notes[i].time > endTime)
                {
                    endTime = notes[i].time;
                }
            }

            return endTime;
        }
    }

    [Serializable]
    public class ChartNote
    {
        public float time;
        public string gesture;
        public int lane;
        public string animal;

        public GestureType GestureType
        {
            get
            {
                if (Enum.TryParse(gesture, true, out GestureType parsed))
                {
                    return parsed;
                }

                return GestureType.Down;
            }
        }
    }
}
