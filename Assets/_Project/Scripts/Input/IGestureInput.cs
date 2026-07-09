namespace MaestroZoo
{
    public interface IGestureInput
    {
        bool TryConsumeGesture(out GestureType gesture, out float inputTime);
    }
}

