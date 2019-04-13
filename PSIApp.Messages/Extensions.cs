using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSIApp
{
    // C# vymysl - moznost definice vlastnit funkci a pak je volat
    // jako by byly napsany pro dany objekt
    public static class Extensions
    {
        public static T Pop<T>(this ICollection<T> collection)
        {
            if (collection.Count > 0)
            {
                T item = collection.First();
                collection.Remove(item);
                return item;
            }
            throw new Exception("Collection is empty.");
        }

        public static bool AreSameArrays<T>(T[] arr1, T[] arr2)
        {
            if (arr1 is null || arr2 is null) return false;

            if (arr1.Length != arr2.Length) return false;

            for(int i = 0; i < arr1.Length; ++i)
            {
                if (!arr1[i].Equals(arr2[i])) return false;
            }
            return true;
        }
    }
}
