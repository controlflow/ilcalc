﻿using System;
using System.Collections.Generic;

namespace ILCalc
{
  sealed partial class Parser<T>
  {
    #region Fields

    int exprDepth, curPos, prePos;
    Stack<Code> curStack;
    T value;

    #endregion

    int Parse(ref int i, bool func)
    {
      Item prev = Item.Begin;
      int separators = 0;
      var operators = new Stack<Code>();
      this.curStack = operators;

      while (i < this.expr.Length)
      {
        char c = this.expr[i];

        if (Char.IsWhiteSpace(c)) { i++; continue; }

        this.curPos = i++;
        int val;

        // ============================================= NUMBER ==
        if ((val = Literal.TryParse(i-1, this)) != -1)
        {
          i += val - 1;

          // [ )123 ], [ 123 456 ] or [ pi123 ]
          if (prev >= Item.Number)
          {
            throw IncorrectConstr(prev, Item.Number, i);
          }

          Output.PutConstant(value);
          prev = Item.Number;
        }

        // =========================================== OPERATOR ==
        else if ((val = Operators.IndexOf(c)) != -1)
        {
          // BINARY ======================
          // [ )+ ], [ 123+ ] or [ pi+ ]
          if (prev >= Item.Number)
          {
            Flush(operators, Priority[val]);
            operators.Push((Code) val);
          }

          // UNARY [-] ===================
          else if (val == (int) Code.Sub)
          {
            // prev == [+-], [,] or [(]
            operators.Push(Code.Neg);
          }

          // UNARY [+] ===================
          else
          {
            throw IncorrectConstr(prev, Item.Operator, i);
          }

          //i++; // <===
          prev = Item.Operator;
        }

        // ========================================== SEPARATOR ==
        else if (c == this.sepSymbol)
        {
          if (!func)
          {
            throw InvalidSeparator();
          }

          // [ (, ], [ +, ] or [ ,, ]
          if (prev <= Item.Begin)
          {
            throw IncorrectConstr(prev, Item.Separator, i);
          }

          Flush(operators);
          Output.PutSeparator();
          separators++;

          //i++; // <====
          prev = Item.Separator;
        }

        // ========================================= BRACE OPEN ==
        else if (c == '(')
        {
          // [ )( ], [ 123( ] or [ pi( ]
          if (prev >= Item.Number)
          {
            if (!Context.ImplicitMul)
            {
              throw IncorrectConstr(prev, Item.Begin, i);
            }

            Flush(operators, 1);
            operators.Push(Code.Mul); // Insert [*]
          }

          //i++; // <======
          ParseNested(ref i, false);
          this.curStack = operators;
          prev = Item.End;
        }

        // ======================================== BRACE CLOSE ==
        else if (c == ')')
        {
          // [ +) ], [ ,) ] or [ () ]
          if (prev <= Item.Separator ||
            (!func && prev == Item.Begin))
          {
            throw IncorrectConstr(prev, Item.End, i);
          }

          Flush(operators);
          if (this.exprDepth == 0)
          {
            throw BraceDisbalance(this.curPos, true);
          }

          if (prev != Item.Begin)
          {
            separators++;
          }

          //i++; // <=====
          return separators;
        }

        // ========================================= IDENTIFIER ==
        else if (Char.IsLetter(c) || c == '_')
        {
          if (prev >= Item.Number)
          {
            // [ pi sin ]
            if (prev == Item.Identifier)
            {
              //TODO: test if "sin z" (1 char unresolved!)
              throw IncorrectIden(i);
            }

            if (!Context.ImplicitMul)
            {
              throw IncorrectConstr(prev, Item.Identifier, i);
            }

            // [ )pi ] or [ 123pi ]
            Flush(operators, 1);
            operators.Push(Code.Mul); // Insert [*]
          }

          prev = ScanIdenifier(ref i);
        }

        // ========================================= UNRESOLVED ==
        else
        {
          throw UnresolvedSymbol(this.curPos);
        }

        this.prePos = this.curPos;
      }

      // ====================================== END OF EXPRESSION ==
      // [ +) ], [ ,) ] or [ () ]
      if (prev <= Item.Begin)
      {
        throw IncorrectConstr(prev, Item.End, i);
      }

      Flush(operators);
      Output.PutExprEnd();

      return -1;
    }

    #region Stack Operations

    void Flush(Stack<Code> stack)
    {
      while (stack.Count > 0)
      {
        Output.PutOperator(stack.Pop());
      }
    }

    void Flush(Stack<Code> stack, int priority)
    {
      while (stack.Count > 0 &&
        priority <= Priority[(int) stack.Peek()])
      {
        Output.PutOperator(stack.Pop());
      }
    }

    #endregion
  }
}