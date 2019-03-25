using System;

namespace Calc
{
    class Program
    {
        static void Main(string[] args)
        {
            string exp = "2*-1";
            double expectedResult = 2*-1;
            double result = new Evaluator().Evaluate(exp);

            Console.WriteLine($"{exp} = {result} ({expectedResult})");

            Console.ReadKey();
        }
    }
}
