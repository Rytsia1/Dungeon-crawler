namespace DungeonCrawler.Interaction
{
    public interface IInteractable
    {
        void Interact();
        string GetPromptText();
    }
}
