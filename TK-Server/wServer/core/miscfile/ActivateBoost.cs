﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace wServer.core
{
    public class ActivateBoost
    {
        private List<int> _base;
        private int _offset;
        private List<int> _stack;

        public ActivateBoost()
        {
            _stack = new List<int>();
            _base = new List<int> { 0 };
        }

        public void AddOffset(int amount) => _offset += amount;

        public int GetBoost()
        {
            var boost = 0;

            for (var i = 0; i < _stack.Count; i++)
                boost += (int)(_stack[_stack.Count - 1 - i] * Math.Pow(.5, i));

            boost += _base[0];
            boost += _offset;

            return boost;
        }

        public void Pop(int amount, bool noStack = false)
        {
            if (noStack)
            {
                _base.Remove(amount);
                return;
            }

            if (_stack.Count <= 0)
                return;

            _stack.Remove(amount);
            _stack.Sort();
        }

        public void Push(int amount, bool noStack = false)
        {
            if (noStack)
            {
                _base.Add(amount);
                _base = _base.OrderByDescending(a => a).ToList();
                return;
            }

            _stack.Add(amount);
            _stack.Sort();
        }
    }
}
