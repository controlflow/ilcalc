﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ILCalc;

namespace SilverCalc
{
  public partial class Page
  {
    #region Fields

    readonly List<string> exprList = new List<string>();
    readonly CalcContext<double> calc;
    int listPos = -1;

    #endregion

    #region Constructor

    public Page()
    {
      InitializeComponent();

      HideInfo.Completed += delegate { infoPanel.Visibility = Visibility.Collapsed; };
      HideError.Completed += delegate { errorPanel.Visibility = Visibility.Collapsed; };

      this.calc = new CalcContext<double>();
      this.calc.Culture = CultureInfo.CurrentCulture;
      this.calc.Functions.ImportBuiltIn();
      this.calc.Constants.ImportBuiltIn();

      System.Linq.Expressions.Expression<Func<string>> func = () => "hello!";

      console.Text = func.Compile()();
    }

    #endregion

    #region Event Handlers

    void launchEvaluate_Click(object sender, RoutedEventArgs e)
    {
      if (infoPanel.Visibility == Visibility.Visible)
      {
        console.IsEnabled = true;
        HideInfo.Begin();
      }

      string expr = expressionBox.Text;
      double res;
      try
      {
        res = this.calc.Evaluate(expr);
      }
      catch (SyntaxException err)
      {
        expressionBox.Select(err.Position, err.Length);
        errorText.Text = err.Message;

        if (errorPanel.Visibility == Visibility.Collapsed)
        {
          errorPanel.Visibility = Visibility.Visible;
          ShowError.Begin();
        }

        return;
      }

      if (errorPanel.Visibility == Visibility.Visible)
      {
        HideError.Begin();
      }

      var buf = new StringBuilder();

      buf.Append(expressionBox.Text);
      buf.Append(" = ");
      buf.Append(res);
      buf.AppendLine();

      console.Text += buf.ToString();
      console.Select(console.Text.Length - 1, 0);

      expressionBox.Text = string.Empty;
      expressionBox.Focus();

      this.exprList.Add(expr);
      this.listPos = this.exprList.Count;
    }

    void expressionBox_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        this.launchEvaluate_Click(null, null);
      }
      else if (this.exprList.Count != 0)
      {
        if (e.Key == Key.Up)
        {
          if (--this.listPos < 0)
          {
            this.listPos = this.exprList.Count - 1;
          }

          this.expressionBox.Text = this.exprList[this.listPos];
        }
        else if (e.Key == Key.Down)
        {
          if (++this.listPos >= this.exprList.Count)
          {
            this.listPos = 0;
          }

          this.expressionBox.Text = this.exprList[this.listPos];
        }
      }
    }

    void consoleClear_Click(object sender, RoutedEventArgs e)
    {
      if (infoPanel.Visibility == Visibility.Visible)
      {
        console.IsEnabled = true;
        HideInfo.Begin();
      }

      console.Text = string.Empty;
      this.exprList.Clear();
      expressionBox.Focus();
    }

    void listFunctions_Click(object sender, RoutedEventArgs e) { this.ListMembers("Available functions:", this.calc.Functions.Names); }

    void listConstants_Click(object sender, RoutedEventArgs e) { this.ListMembers("Available constants:", this.calc.Constants.Keys); }

    #endregion

    #region Methods

    void ListMembers(string str, IEnumerable<string> names)
    {
      var buf = new StringBuilder();
      buf.AppendLine(str);

      bool comma = false;
      foreach (string name in names)
      {
        if (comma)
        {
          buf.Append(", ");
        }
        else
        {
          comma = true;
        }

        buf.Append(name);
      }

      lbInfoText.Text = buf.ToString();

      if (infoPanel.Visibility == Visibility.Collapsed)
      {
        console.IsEnabled = false;
        ShowInfo.Begin();
        infoPanel.Visibility = Visibility.Visible;
      }
    }

    #endregion
  }
}