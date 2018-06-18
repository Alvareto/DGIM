using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DGIM
{
    static class Program
    {
        private const char QUERY_SIGN = 'q';
        private const char BIT_1 = '1';
        private const double DIVIDER = 2d;
        private const int MAX__SAME_BUCKET_SIZE__COUNT = 2;
        private const int POWER_BASE = 2;


        public static void Merge<T>(this List<T> list) where T : Bucket
        {
            var hasBucketsToMerge = list.Count > MAX__SAME_BUCKET_SIZE__COUNT; // >= 3;
            var checkFromIndex = list.Count - 1;

            while (hasBucketsToMerge)
            {
                hasBucketsToMerge = false;

                var consecutiveCount = 0;
                for (var i = checkFromIndex + 1; i-- != 0;)
                {
                    if (list[checkFromIndex].Size == list[i].Size)
                    {
                        consecutiveCount++;
                        if (consecutiveCount == MAX__SAME_BUCKET_SIZE__COUNT + 1)
                        {
                            hasBucketsToMerge = true;
                            break;
                        }
                    }
                }

                if (hasBucketsToMerge)
                {
                    var index = checkFromIndex - MAX__SAME_BUCKET_SIZE__COUNT;
                    list.RemoveAt(index);
                    list[index].Size *= POWER_BASE;
                }

                checkFromIndex -= MAX__SAME_BUCKET_SIZE__COUNT;

                hasBucketsToMerge &= checkFromIndex >= MAX__SAME_BUCKET_SIZE__COUNT;
            }
        }

        public static void Add<T>(this List<T> list)
            where T : Bucket, new()
        {
            list.Add(new T() { Timestamp = 1, Size = 1 });

            list.Merge();
        }

        public static void Tick<T>(this List<T> list)
            where T : Bucket
        {
            foreach (var bucket in list)
            {
                bucket.Timestamp++;
            }
        }

        public static void CheckWindow<T>(this List<T> list, int N)
            where T : Bucket
        {
            if (list.Any() && list[0].Timestamp > N)
            {
                list.RemoveAt(0); // remove first
            }
        }

        public static int Query<T>(this IQueryable<T> list, int k, Expression<Func<T, bool>> filter, Expression<Func<T, int>> accumulator)
            where T : Bucket
        {
            //// 1. pronači najstariji pretinac z čija vremenska oznaka još uvijek pripada prozoru od k
            //// 2. sumirati veličine svih pretinaca s novijim vremenskim oznakama od one pretinca z
            //// 3. dodati sumi iz 2. pola veličine pretinca z (zaokruženo na manji broj)

            // 1. first where b.time < (max - k)
            var z = list.FirstOrDefault(filter); // b => b.Timestamp < k

            if (z == null)
            {
                return 0;
            }

            // 2. sum where b.time < z
            var sumFilter = list.Where(b => b.Timestamp < z.Timestamp);
            var sum = sumFilter.Sum(accumulator); // b => b.Size

            // 3. add z/2
            sum += (int)Math.Floor(z.Size / DIVIDER);

            return sum;
        }

        public class Bucket
        {
            public int Timestamp { get; set; }
            public int Size { get; set; }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Bucket)obj);
            }

            protected bool Equals(Bucket other)
            {
                return Timestamp == other.Timestamp && Size == other.Size;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Timestamp * 397) ^ Size;
                }
            }

            public static bool operator ==(Bucket left, Bucket right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Bucket left, Bucket right)
            {
                return !Equals(left, right);
            }
        }

        static void Main(string[] args)
        {
            var N = Convert.ToInt32(Console.ReadLine());

            var Buckets = new List<Bucket>();

            string line;
            while ((line = Console.ReadLine()) != String.Empty && line != null)
            {
                if (line.First() == QUERY_SIGN)
                {
                    var k = Convert.ToInt32(line.Substring(2));

                    var sum = Buckets.AsQueryable().Query(k, b => b.Timestamp <= k, b => b.Size);

                    Console.WriteLine(sum);
                }
                else
                {
                    foreach (var bit in line)
                    {
                        Buckets.Tick();

                        Buckets.CheckWindow(N);

                        if (bit != BIT_1)
                        {
                            continue;
                        }

                        Buckets.Add();
                    }
                }
            }
        }
    }
}
