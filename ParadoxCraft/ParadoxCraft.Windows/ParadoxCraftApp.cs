using ParadoxCraft.Generation;

namespace ParadoxCraft
{
    public class ParadoxCraftApp
    {
        private static ParadoxCraftGame Game;

        private static void Main(string[] args)
        {
            ChunkLoader.Initialize();
            using (Game = new ParadoxCraftGame())
            {
                Game.Factory.RequestChunk += ChunkLoader.RequestChunk;
                ChunkLoader.ProcessChunk += Game.Factory.ProcessChunk;
                Game.Run();
            }
        }
    }
}
