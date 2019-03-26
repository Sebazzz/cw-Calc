using System;

namespace Calc
{
    class Program
    {
        static void Main(string[] args)
        {
            string exp = "-123";
            double expectedResult = -123;

            double result = new Evaluator().Evaluate(exp);

            Console.WriteLine($"{exp} = {result} ({expectedResult})");

            Console.ReadKey();
        }
    }
}
