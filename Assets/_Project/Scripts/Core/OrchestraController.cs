using System.Collections.Generic;
using UnityEngine;

namespace MaestroZoo
{
    public class OrchestraController : MonoBehaviour
    {
        public JudgeManager judgeManager;
        public Light stageLight;

        public float Mood { get; private set; } = 60f;
        public float Fever { get; private set; }
        public bool FeverActive { get; private set; }

        private readonly Dictionary<string, AnimalPerformer> animals = new Dictionary<string, AnimalPerformer>();
        private JudgeManager subscribedJudge;

        private void Update()
        {
            UpdateSubscription();

            Fever = Mathf.MoveTowards(Fever, 0f, Time.deltaTime * (FeverActive ? 2.5f : 0.45f));
            if (FeverActive && Fever <= 0.1f)
            {
                FeverActive = false;
            }

            if (stageLight != null)
            {
                float mood01 = Mathf.Clamp01(Mood / 100f);
                stageLight.intensity = 0.7f + mood01 * 1.1f + (FeverActive ? 1.1f : 0f);
            }

            foreach (AnimalPerformer animal in animals.Values)
            {
                if (animal != null)
                {
                    animal.SetMood(Mathf.Clamp01(Mood / 100f));
                }
            }
        }

        private void OnDisable()
        {
            if (subscribedJudge != null)
            {
                subscribedJudge.NoteJudged -= HandleNoteJudged;
                subscribedJudge.WrongGesture -= HandleWrongGesture;
                subscribedJudge.JudgementReset -= ResetOrchestra;
                subscribedJudge = null;
            }
        }

        public void Register(AnimalPerformer animal)
        {
            if (animal == null || string.IsNullOrEmpty(animal.animalId))
            {
                return;
            }

            animals[animal.animalId] = animal;
        }

        public void FreeGesture(GestureType gesture)
        {
            AnimalPerformer animal = FindAnimalForGesture(gesture);
            if (animal != null)
            {
                animal.Hit(0.65f);
            }

            Mood = Mathf.Clamp(Mood + 1.5f, 0f, 100f);
        }

        public string MoodName()
        {
            if (Mood < 30f)
            {
                return "Chaotic";
            }

            if (Mood < 60f)
            {
                return "Stable";
            }

            if (Mood < 88f)
            {
                return "Excited";
            }

            return "Perfect Sync";
        }

        private void UpdateSubscription()
        {
            if (subscribedJudge == judgeManager)
            {
                return;
            }

            if (subscribedJudge != null)
            {
                subscribedJudge.NoteJudged -= HandleNoteJudged;
                subscribedJudge.WrongGesture -= HandleWrongGesture;
                subscribedJudge.JudgementReset -= ResetOrchestra;
            }

            subscribedJudge = judgeManager;

            if (subscribedJudge != null)
            {
                subscribedJudge.NoteJudged += HandleNoteJudged;
                subscribedJudge.WrongGesture += HandleWrongGesture;
                subscribedJudge.JudgementReset += ResetOrchestra;
            }
        }

        private void HandleNoteJudged(FlyingNote note, JudgeResult result)
        {
            if (note == null || note.Note == null)
            {
                return;
            }

            AnimalPerformer animal = FindAnimal(note.Note.animal, note.Note.GestureType);
            if (animal != null)
            {
                animal.React(result);
            }

            if (result == JudgeResult.Perfect)
            {
                Mood = Mathf.Clamp(Mood + 4f, 0f, 100f);
                Fever = Mathf.Clamp(Fever + 8f, 0f, 100f);
            }
            else if (result == JudgeResult.Good)
            {
                Mood = Mathf.Clamp(Mood + 2f, 0f, 100f);
                Fever = Mathf.Clamp(Fever + 4f, 0f, 100f);
            }
            else
            {
                Mood = Mathf.Clamp(Mood - 9f, 0f, 100f);
                Fever = Mathf.Clamp(Fever - 12f, 0f, 100f);
            }

            if (!FeverActive && Fever >= 100f)
            {
                FeverActive = true;
                Fever = 100f;
            }
        }

        private void HandleWrongGesture(GestureType gesture)
        {
            Mood = Mathf.Clamp(Mood - 4f, 0f, 100f);
            AnimalPerformer animal = FindAnimalForGesture(gesture);
            if (animal != null)
            {
                animal.Miss();
            }
        }

        private void ResetOrchestra()
        {
            Mood = 60f;
            Fever = 0f;
            FeverActive = false;
        }

        private AnimalPerformer FindAnimal(string animalId, GestureType fallbackGesture)
        {
            if (!string.IsNullOrEmpty(animalId) && animals.TryGetValue(animalId, out AnimalPerformer animal))
            {
                return animal;
            }

            if (animalId == "FullOrchestra")
            {
                foreach (AnimalPerformer performer in animals.Values)
                {
                    if (performer != null)
                    {
                        performer.Hit(0.8f);
                    }
                }

                return null;
            }

            return FindAnimalForGesture(fallbackGesture);
        }

        private AnimalPerformer FindAnimalForGesture(GestureType gesture)
        {
            string id = "RabbitDrum";
            if (gesture == GestureType.Up)
            {
                id = "BirdFlute";
            }
            else if (gesture == GestureType.Left)
            {
                id = "FoxViolin";
            }
            else if (gesture == GestureType.Right)
            {
                id = "BearCello";
            }
            else if (gesture == GestureType.Expand || gesture == GestureType.Close)
            {
                id = "ElephantHorn";
            }

            animals.TryGetValue(id, out AnimalPerformer animal);
            return animal;
        }
    }
}

