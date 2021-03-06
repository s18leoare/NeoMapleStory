﻿using System.Threading;

namespace NeoMapleStory.Core
{
    public class InterLockedInt
    {
        private int m_interValue;

        public InterLockedInt(int value)
        {
            m_interValue = value;
        }

        public int Value => m_interValue;

        /// <summary>
        ///     递增变量
        /// </summary>
        /// <returns></returns>
        public int Increment()
        {
            return Interlocked.Increment(ref m_interValue);
        }

        /// <summary>
        ///     递减变量
        /// </summary>
        /// <returns></returns>
        public int Decrement()
        {
            return Interlocked.Decrement(ref m_interValue);
        }

        /// <summary>
        ///     对两个32位整数求和并用和替换原值，返回被存储的新值。
        /// </summary>
        /// <param name="value">要添加到整数中的值</param>
        /// <returns>被储存的新值</returns>
        public int Add(int value)
        {
            var result = Interlocked.Add(ref m_interValue, value);
            return result;
        }

        /// <summary>
        ///     将原值设置为指定的新值，返回原始值。
        /// </summary>
        /// <param name="value">要被设置的新值</param>
        /// <returns>原始值</returns>
        public int Exchange(int value)
        {
            var result = Interlocked.Exchange(ref m_interValue, value);
            return result;
        }

        /// <summary>
        ///     将值归零
        /// </summary>
        public void Reset()
        {
            if (m_interValue != 0)
                Exchange(0);
        }
    }
}