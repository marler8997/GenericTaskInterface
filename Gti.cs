/*

Generic Task Interface
-----------------------------------
1. A specification to configure interfaces for generic tasks
2. A specification to map a generic task interface to a command line tool

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gti
{
    public enum ParameterType
    {
        Enum,
        File,
        InputFile,
        Directory,
        OutputDirectory,
        Count,
    }
    public static class Extensions
    {
        public static UInt32 SafeLength<T>(this ICollection<T> collection)
        {
            return (collection == null) ? 0 : (uint)collection.Count;
        }
        public static IEnumerable<T> SafeEnumerable<T>(this IEnumerable<T> enumerable)
        {
            return (enumerable == null) ? EmptyEnumerable<T>.Instance : enumerable;
        }
    }
    public class EmptyEnumerable<T> : IEnumerable<T>
    {
        public static readonly EmptyEnumerable<T> Instance = new EmptyEnumerable<T>();
        private EmptyEnumerable()
        {
        }
        public IEnumerator<T> GetEnumerator()
        {
            return EmptyEnumerator<T>.Instance;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return EmptyEnumerator<T>.Instance;
        }
    }
    public class EmptyEnumerator<T> : IEnumerator<T>
    {
        public static readonly EmptyEnumerator<T> Instance = new EmptyEnumerator<T>();
        private EmptyEnumerator()
        {
        }
        public T Current
        {
            get { throw new InvalidOperationException(); }
        }
        object System.Collections.IEnumerator.Current
        {
            get { throw new InvalidOperationException(); }
        }
        public void Dispose()
        {
        }
        public bool MoveNext()
        {
            return false;
        }
        public void Reset()
        {
        }
    }
}
