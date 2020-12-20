using OpenTK;

namespace GraphicsAssignment1
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Game game = new Game(1920, 1080, "Rainbow Donut Party"))
            {
                game.Cursor = MouseCursor.Empty;
                game.Run(60.0);
            }
        }
    }
}
