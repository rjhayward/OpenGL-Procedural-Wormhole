﻿using OpenTK;

namespace GraphicsAssignment2
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Game game = new Game(1600, 900, "Space Wormhole Simulator"))
            {
                game.Cursor = MouseCursor.Empty;

                //60 updates per second, 60 frames per second 
                game.Run(60.0, 60.0);
            }
        }
    }
}
