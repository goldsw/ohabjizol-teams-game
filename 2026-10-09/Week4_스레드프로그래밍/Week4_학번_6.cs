using System;
using System.Threading;

namespace ThreadLockStudy
{
    // 추가 학습: 여러 스레드가 같은 변수를 고칠 때 lock이 필요한 이유
    // 과제 3은 작업 스레드가 1개라 Join만으로 결과를 안전하게 읽을 수 있다.
    // 스레드가 여러 개면 sum += 1 이 겹치면서 값이 사라질 수 있다.
    class Program
    {
        const int THREADS = 4;
        const int LOOPS = 200000;

        static int unsafeSum = 0;
        static int safeSum = 0;
        static readonly object gate = new object();

        static void AddUnsafe(object arg)
        {
            for (int i = 0; i < LOOPS; i++)
            {
                int local = unsafeSum;
                unsafeSum = local + 1;
            }
        }

        static void AddSafe(object arg)
        {
            for (int i = 0; i < LOOPS; i++)
            {
                lock (gate)
                {
                    safeSum += 1;
                }
            }
        }

        static void Main(string[] args)
        {
            int expected = THREADS * LOOPS;

            Console.WriteLine("추가 학습: 공유 변수와 lock");
            Console.WriteLine("스레드 {0}개, 스레드당 증가 {1}회", THREADS, LOOPS);
            Console.WriteLine("기대값 = {0}", expected);
            Console.WriteLine();

            Thread[] unsafeWorkers = new Thread[THREADS];
            for (int i = 0; i < THREADS; i++)
            {
                unsafeWorkers[i] = new Thread(AddUnsafe);
                unsafeWorkers[i].Start();
            }
            for (int i = 0; i < THREADS; i++)
                unsafeWorkers[i].Join();
            Console.WriteLine("lock 없음  결과 = {0}", unsafeSum);

            Thread[] safeWorkers = new Thread[THREADS];
            for (int i = 0; i < THREADS; i++)
            {
                safeWorkers[i] = new Thread(AddSafe);
                safeWorkers[i].Start();
            }
            for (int i = 0; i < THREADS; i++)
                safeWorkers[i].Join();
            Console.WriteLine("lock 사용  결과 = {0}", safeSum);
        }
    }
}
