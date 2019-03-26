using System;

namespace Calc
{
    class Program
    {
        static void Main(string[] args)
        {
            string exp = "((80 - (19)))";
            double expectedResult = ((80 - (19)));

            double result = new Evaluator().Evaluate(exp);

            Console.WriteLine($"{exp} = {result} ({expectedResult})");

            Console.ReadKey();
        }
    }
}
