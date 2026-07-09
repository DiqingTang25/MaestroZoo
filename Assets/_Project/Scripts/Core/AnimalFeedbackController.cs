using UnityEngine;

namespace MaestroZoo
{
    public class AnimalFeedbackController : MonoBehaviour
    {
        public JudgeManager judgeManager;
        public Animator rabbitDrum;
        public Animator foxViolin;
        public Animator bearCello;
        public Animator birdFlute;

        private void OnEnable()
        {
            if (judgeManager != null)
            {
                judgeManager.NoteJudged += HandleNoteJudged;
            }
        }

        private void OnDisable()
        {
            if (judgeManager != null)
            {
                judgeManager.NoteJudged -= HandleNoteJudged;
            }
        }

        private void HandleNoteJudged(FlyingNote note, JudgeResult result)
        {
            Animator animator = FindAnimator(note.Note.animal);
            if (animator == null)
            {
                return;
            }

            string triggerName = result == JudgeResult.Miss ? "Miss" : "Hit";
            animator.SetTrigger(triggerName);
        }

        private Animator FindAnimator(string animal)
        {
            return animal switch
            {
                "RabbitDrum" => rabbitDrum,
                "FoxViolin" => foxViolin,
                "BearCello" => bearCello,
                "BirdFlute" => birdFlute,
                _ => null
            };
        }
    }
}

