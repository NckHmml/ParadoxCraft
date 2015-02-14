
namespace ParadoxCraft
{
    class ParadoxCraftApp
    {
        static void Main(string[] args)
        {
            // Profiler.EnableAll();
            using (var game = new ParadoxCraftGame())
            {
                game.Run();
            }
        }
    }
}
