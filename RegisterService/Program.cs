using System;

namespace RegisterService
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            var mainService = new MainProcess();
            mainService.Run();

        }
    }
}
