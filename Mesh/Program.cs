using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Mesh
{
    public static class Program
    {
        private static void Main()
        {
            using (Window window = new Window(1600,1200,"Mesh - Demo"))
            {
                window.Run();
            }
        }
    }
}