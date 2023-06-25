using System.Diagnostics;

namespace SoftRender.App
{
    public partial class MainForm
    {
        private class MovingAverage
        {
            private Queue<int> ints = new Queue<int>();

            public int Size { get; private set; }

            public MovingAverage(int size)
            {
                Debug.Assert(size > 0);

                Size = size;
            }

            public void Push(int value)
            {
                ints.Enqueue(value);
                if(ints.Count > Size)
                {
                    ints.Dequeue();
                }
            }

            public int GetAverage()
            {
                if(ints.Count == 0)
                {
                    return 0;
                }

                int sum = 0;
                for (int i = 0; i < ints.Count; i++)
                {
                    sum += ints.ElementAt(i);
                }

                return sum / ints.Count;
            }
        }
    }
}