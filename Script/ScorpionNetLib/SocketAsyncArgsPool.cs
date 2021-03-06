﻿
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace ScorpionNetLib
{
    internal sealed class SocketAsyncEventArgsPool
    {
        //just for assigning an ID so we can watch our objects while testing.
        private Int32 nextTokenId = 0;

        // Pool of reusable SocketAsyncEventArgs objects.        
        Stack<SocketAsyncEventArgs> pool;

        // initializes the object pool to the specified size.
        // "capacity" = Maximum number of SocketAsyncEventArgs objects
        internal SocketAsyncEventArgsPool(Int32 capacity)
        {
            pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        ManualResetEvent poolEvent = new ManualResetEvent(true);

        // The number of SocketAsyncEventArgs instances in the pool.         
        internal Int32 Count
        {
            get { return pool.Count; }
        }

        // Removes a SocketAsyncEventArgs instance from the pool.
        // returns SocketAsyncEventArgs removed from the pool.
        internal SocketAsyncEventArgs Pop()
        {
            lock (pool)
            {
                if (pool.Count == 0)
                {
                    poolEvent.Reset();
                }
            }

            poolEvent.WaitOne();

            lock (pool)
            {
                return pool.Pop();
            }
        }

        // Add a SocketAsyncEventArg instance to the pool. 
        // "item" = SocketAsyncEventArgs instance to add to the pool.
        internal void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
            }
            lock (pool)
            {
                pool.Push(item);
                poolEvent.Set();
            }
        }
    }
}
