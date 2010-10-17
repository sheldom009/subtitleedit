﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Nikse.SubtitleEdit.Logic;
using Nikse.SubtitleEdit.Logic.OCR;
using System.Text;

namespace Nikse.SubtitleEdit.Forms
{
    public sealed partial class FixCommonErrors : Form
    {
        const int IndexRemoveEmptyLines = 0;
        const int IndexOverlappingDisplayTime = 1;
        const int IndexTooShortDisplayTime = 2;
        const int IndexTooLongDisplayTime = 3;
        const int IndexInvalidItalicTags = 4;
        const int IndexUnneededSpaces = 5;
        const int IndexUnneededPeriods = 6;
        const int IndexMissingSpaces = 7;
        const int IndexBreakLongLines = 8;
        const int IndexMergeShortLines = 9;
        const int IndexUppercaseIInsideLowercaseWord = 10;
        const int IndexDoubleApostropheToQuote = 11;
        const int IndexFixMusicNotation = 12;
        const int IndexAddPeriodAfterParagraph = 13;
        const int IndexStartWithUppercaseLetterAfterParagraph = 14;
        const int IndexStartWithUppercaseLetterAfterPeriodInsideParagraph = 15;
        const int IndexAddMissingQuotes = 16;
        const int IndexFixHyphens = 17;
        const int IndexFix3PlusLines = 18;
        const int IndexFixDoubleDash = 19;
        const int IndexFixDoubleGreaterThan = 20;
        const int IndexFixEllipsesStart = 21;
        const int IndexFixMissingOpenBracket = 22;
        const int IndexAloneLowercaseIToUppercaseIEnglish = 23;
        const int IndexFixOcrErrorsViaReplaceList = 24;
        const int IndexDanishLetterI = 25;
        const int IndexFixSpanishInvertedQuestionAndExclamationMarks = 27;

        int _danishLetterIIndex = -1;
        int _spanishInvertedQuestionAndExclamationMarksIndex = -1;

        readonly LanguageStructure.FixCommonErrors _language;
        readonly LanguageStructure.General _languageGeneral;
        private bool _hasFixesBeenMade;

        class FixItem
        {
            public string Name { get; set; }
            public string Example { get; set; }
            public EventHandler Action { get; set; }
            public bool DefaultChecked { get; set; }

            public FixItem(string name, string example, EventHandler action, bool selected)
            {
                Name = name;
                Example = example;
                Action = action;
                DefaultChecked = selected;
            }
        }

        class ListViewSorter : System.Collections.IComparer
        {
            public int Compare(object o1, object o2)
            {
                var lvi1 = o1 as ListViewItem;
                var lvi2 = o2 as ListViewItem;
                if (lvi1 == null || lvi2 == null)
                    return 0;

                if (Descending)
                {
                    ListViewItem temp = lvi1;
                    lvi1 = lvi2;
                    lvi2 = temp;
                }

                if (IsNumber)
                {
                    int i1 = int.Parse(lvi1.SubItems[ColumnNumber].Text);
                    int i2 = int.Parse(lvi2.SubItems[ColumnNumber].Text);

                    if (i1 > i2)
                        return 1;
                    if (i1 == i2)
                        return 0;
                    return -1;
                }
                return string.Compare(lvi2.SubItems[ColumnNumber].Text, lvi1.SubItems[ColumnNumber].Text);
            }
            public int ColumnNumber { get;  set; }
            public bool IsNumber { get; set; }
            public bool Descending { get; set; }
        }   

        Subtitle _subtitle;
        Subtitle _originalSubtitle;
        int _totalFixes;
        int _totalErrors;
        List<FixItem> _fixActions;
        int _subtitleListViewIndex = -1;
        bool _onlyListFixes = true;
        string _autoDetectGoogleLanguage;
        List<string> _namesEtcList;
        List<string> _abbreviationList;
        StringBuilder _newLog = new StringBuilder();
        StringBuilder _appliedLog = new StringBuilder();
        private int _numberOfImportantLogMessages = 0;

        public Subtitle FixedSubtitle
        {
            get { return _originalSubtitle; }
        }

        public void Initialize(Subtitle subtitle)
        {
            _autoDetectGoogleLanguage = Utilities.AutoDetectGoogleLanguage(subtitle);
            CultureInfo ci = CultureInfo.GetCultureInfo(_autoDetectGoogleLanguage);
            string threeLetterISOLanguageName = ci.ThreeLetterISOLanguageName;

            FixCommonErrorsSettings ce = Configuration.Settings.CommonErrors;

            _fixActions = new List<FixItem>();
            _fixActions.Add(new FixItem(_language.RemovedEmptyLinesUnsedLineBreaks, string.Empty, delegate { FixEmptyLines(); }, ce.EmptyLinesTicked));
            _fixActions.Add(new FixItem(_language.FixOverlappingDisplayTimes, string.Empty, delegate { FixOverlappingDisplayTimes(); }, ce.OverlappingDisplayTimeTicked));
            _fixActions.Add(new FixItem(_language.FixShortDisplayTimes, string.Empty, delegate { FixShortDisplayTimes(); }, ce.TooShortDisplayTimeTicked));
            _fixActions.Add(new FixItem(_language.FixLongDisplayTimes, string.Empty, delegate { FixLongDisplayTimes(); }, ce.TooLongDisplayTimeTicked));
            _fixActions.Add(new FixItem(_language.FixInvalidItalicTags, _language.FixInvalidItalicTagsExample, delegate { FixInvalidItalicTags(); }, ce.InvalidItalicTagsTicked));
            _fixActions.Add(new FixItem(_language.RemoveUnneededSpaces, _language.RemoveUnneededSpacesExample, delegate { FixUnneededSpaces(); }, ce.UnneededSpacesTicked));
            _fixActions.Add(new FixItem(_language.RemoveUnneededPeriods, _language.RemoveUnneededPeriodsExample, delegate { FixUnneededPeriods(); }, ce.UnneededPeriodsTicked));
            _fixActions.Add(new FixItem(_language.FixMissingSpaces, _language.FixMissingSpacesExample, delegate { FixMissingSpaces(); }, ce.MissingSpacesTicked));
            _fixActions.Add(new FixItem(_language.BreakLongLines, string.Empty, delegate { FixLongLines(); }, ce.BreakLongLinesTicked));
            _fixActions.Add(new FixItem(_language.RemoveLineBreaks, string.Empty, delegate { FixShortLines(); }, ce.MergeShortLinesTicked));
            _fixActions.Add(new FixItem(_language.FixUppercaseIInsindeLowercaseWords, _language.FixUppercaseIInsindeLowercaseWordsExample, delegate { FixUppercaseIInsideWords(); }, ce.UppercaseIInsideLowercaseWordTicked));
            _fixActions.Add(new FixItem(_language.FixDoubleApostrophes, string.Empty, delegate { FixDoubleApostrophes(); }, ce.DoubleApostropheToQuoteTicked));
            _fixActions.Add(new FixItem(_language.FixMusicNotation, _language.FixMusicNotationExample, delegate { FixMusicNotation(); }, ce.FixMusicNotationTicked));    
            _fixActions.Add(new FixItem(_language.AddPeriods, string.Empty, delegate { FixMissingPeriodsAtEndOfLine(); }, ce.AddPeriodAfterParagraphTicked));
            _fixActions.Add(new FixItem(_language.StartWithUppercaseLetterAfterParagraph, string.Empty, delegate { FixStartWithUppercaseLetterAfterParagraph(); }, ce.StartWithUppercaseLetterAfterParagraphTicked));
            _fixActions.Add(new FixItem(_language.StartWithUppercaseLetterAfterPeriodInsideParagraph, string.Empty, delegate { FixStartWithUppercaseLetterAfterPeriodInsideParagraph(); }, ce.StartWithUppercaseLetterAfterPeriodInsideParagraphTicked));
            _fixActions.Add(new FixItem(_language.AddMissingQuotes, _language.AddMissingQuotesExample, delegate { AddMissingQuotes(); }, ce.AddMissingQuotesTicked));
            _fixActions.Add(new FixItem(_language.FixHyphens, string.Empty, delegate { FixHyphens(); }, ce.FixHyphensTicked));
            _fixActions.Add(new FixItem(_language.Fix3PlusLines, string.Empty, delegate { Fix3PlusLines(); }, ce.Fix3PlusLinesTicked));
            _fixActions.Add(new FixItem(_language.FixDoubleDash, _language.FixDoubleDashExample, delegate { FixDoubleDash(); }, ce.FixDoubleDashTicked));
            _fixActions.Add(new FixItem(_language.FixDoubleGreaterThan, _language.FixDoubleGreaterThanExample, delegate { FixDoubleGreaterThan(); }, ce.FixDoubleGreaterThanTicked));
            _fixActions.Add(new FixItem(_language.FixEllipsesStart, _language.FixEllipsesStartExample, delegate { FixEllipsesStart(); }, ce.FixEllipsesStartTicked));
            _fixActions.Add(new FixItem(_language.FixMissingOpenBracket, _language.FixMissingOpenBracketExample, delegate { FixMissingOpenBracket(); }, ce.FixMissingOpenBracketTicked));
            _fixActions.Add(new FixItem(_language.FixLowercaseIToUppercaseI, _language.FixLowercaseIToUppercaseIExample, delegate { FixAloneLowercaseIToUppercaseI(); }, ce.AloneLowercaseIToUppercaseIEnglishTicked));
            _fixActions.Add(new FixItem(_language.FixCommonOcrErrors, "D0n't -> Don't", delegate { FixOcrErrorsViaReplaceList(threeLetterISOLanguageName); }, ce.FixOcrErrorsViaReplaceListTicked));

            if (_autoDetectGoogleLanguage == "da" || subtitle.Paragraphs.Count < 25) // && Thread.CurrentThread.CurrentCulture.Name == "da-DK" && 
            {
                _danishLetterIIndex = _fixActions.Count;
                _fixActions.Add(new FixItem(_language.FixDanishLetterI, "Jeg synes i er søde. -> Jeg synes I er søde.", delegate { FixDanishLetterI(); }, ce.DanishLetterITicked));
            }

            if (_autoDetectGoogleLanguage == "es" || subtitle.Paragraphs.Count < 25) // && Thread.CurrentThread.CurrentCulture.Name.StartsWith("es") ||
            {
                _spanishInvertedQuestionAndExclamationMarksIndex = _fixActions.Count;
                _fixActions.Add(new FixItem(_language.FixSpanishInvertedQuestionAndExclamationMarks, "Hablas bien castellano? -> ¿Hablas bien castellano?", delegate { FixSpanishInvertedQuestionAndExclamationMarks(); }, ce.SpanishInvertedQuestionAndExclamationMarksTicked));
            }

            foreach (FixItem fi in _fixActions)
                AddFixActionItemToListView(fi);

            _originalSubtitle = new Subtitle(subtitle); // copy constructor
            _subtitle = new Subtitle(subtitle); // copy constructor
            labelStatus.Text = string.Empty;
            labelTextLineLengths.Text = string.Empty;
            labelTextLineTotal.Text = string.Empty;
            groupBoxStep1.BringToFront();
            groupBox2.Visible = false;
            groupBoxStep1.Visible = true;
            listView1.Columns[0].Width = 50;
            listView1.Columns[1].Width = 310;
            listView1.Columns[2].Width = 400;

            Utilities.InitializeSubtitleFont(textBoxListViewText);
            listViewFixes.ListViewItemSorter = new ListViewSorter { ColumnNumber = 1, IsNumber = true };

            if (Screen.PrimaryScreen.WorkingArea.Width <= 124)
            {
                this.Width = this.MinimumSize.Width;
                this.Height = this.MinimumSize.Height;
            }
        }

        public FixCommonErrors()
        {
            InitializeComponent();

            labelStartTimeWarning.Text = string.Empty;
            labelDurationWarning.Text = string.Empty;
            labelNumberOfImportantLogMessages.Text = string.Empty;

            _language = Configuration.Settings.Language.FixCommonErrors;
            _languageGeneral = Configuration.Settings.Language.General;
            Text = _language.Title;
            groupBoxStep1.Text = _language.Step1;
            groupBox2.Text = _language.Step2;
            listView1.Columns[0].Text = Configuration.Settings.Language.General.Apply;
            listView1.Columns[1].Text = _language.WhatToFix;
            listView1.Columns[2].Text = _language.Example;
            buttonSelectAll.Text = _language.SelectAll;
            buttonInverseSelection.Text = _language.InverseSelection;
            tabControl1.TabPages[0].Text = _language.Fixes;
            tabControl1.TabPages[1].Text = _language.Log;
            listViewFixes.Columns[0].Text = Configuration.Settings.Language.General.Apply;
            listViewFixes.Columns[1].Text = _language.LineNumber;
            listViewFixes.Columns[2].Text = _language.Function;
            listViewFixes.Columns[3].Text = _language.Before;
            listViewFixes.Columns[4].Text = _language.After;
            buttonNextFinish.Text = _language.Next;
            buttonBack.Text = _language.Back;
            buttonCancel.Text = _languageGeneral.Cancel;
            buttonFixesSelectAll.Text = _language.SelectAll;
            buttonFixesInverse.Text = _language.InverseSelection;
            buttonRefreshFixes.Text = _language.RefreshFixes;
            buttonFixesApply.Text = _language.ApplyFixes;
            labelStartTime.Text = _languageGeneral.StartTime;
            labelDuration.Text = _languageGeneral.Duration;
            buttonAutoBreak.Text = _language.AutoBreak;
            buttonUnBreak.Text = _language.Unbreak;
            subtitleListView1.InitializeLanguage(_languageGeneral, Configuration.Settings);
        }

        private void AddFixActionItemToListView(FixItem fi)
        {
            ListViewItem item = new ListViewItem(string.Empty);
            item.Tag = fi;
            item.Checked = fi.DefaultChecked;

            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem(item, fi.Name);
            item.SubItems.Add(subItem);
            subItem = new ListViewItem.ListViewSubItem(item, fi.Example);
            item.SubItems.Add(subItem);

            listView1.Items.Add(item);
        }

        private void AddFixToListView(Paragraph p, int lineNumber, string action, string before, string after)
        {
            if (_onlyListFixes)
            {
                var item = new ListViewItem(string.Empty) { Checked = true };

                var subItem = new ListViewItem.ListViewSubItem(item, lineNumber.ToString());
                item.SubItems.Add(subItem);
                subItem = new ListViewItem.ListViewSubItem(item, action);
                item.SubItems.Add(subItem);
                subItem = new ListViewItem.ListViewSubItem(item, before.Replace(Environment.NewLine, Configuration.Settings.General.ListViewLineSeparatorString));
                item.SubItems.Add(subItem);
                subItem = new ListViewItem.ListViewSubItem(item, after.Replace(Environment.NewLine, Configuration.Settings.General.ListViewLineSeparatorString));
                item.SubItems.Add(subItem);

                item.Tag = p; // save paragraph in Tag

                listViewFixes.Items.Add(item);
            }
        }

        public bool AllowFix(int lineNumber, string action)
        {
             
            //if (!buttonBack.Enabled)
            if (_onlyListFixes)
                return true;

            string ln = lineNumber.ToString();
            foreach (ListViewItem item in listViewFixes.Items)
            {
                if (item.SubItems[1].Text == ln && item.SubItems[2].Text == action)
                    return item.Checked;
            }
            return false;
        }

        public void ShowStatus(string message)
        {
            message = message.Replace(Environment.NewLine, "  ");
            if (message.Length > 83)
                message = message.Substring(0, 80) + "...";
            labelStatus.Text = message;
            labelStatus.Refresh();
        }

        public void LogStatus(string sender, string message, bool isImportant)
        {
            if (isImportant)
                _numberOfImportantLogMessages++;
            LogStatus(sender, message);
        }

        public void LogStatus(string sender, string message)
        {
            if (!string.IsNullOrEmpty(message))            
            {
                message += Environment.NewLine;
                if (_onlyListFixes)
                    _newLog.AppendLine(" +  " + sender + ": " +  message);
                else
                    _appliedLog.AppendLine(Configuration.Settings.Language.General.OK.Replace("&", string.Empty) + " -  " + sender + ": " + message);
            }
        }

        private void FixEmptyLines()
        {
            string fixAction0 = _language.RemovedEmptyLine;
            string fixAction1 = _language.RemovedEmptyLineAtTop;
            string fixAction2 = _language.RemovedEmptyLineAtBottom;

            if (_subtitle.Paragraphs.Count == 0)
                return;

            int emptyLinesRemoved = 0;

            int firstNumber = _subtitle.Paragraphs[0].Number;

            for (int i=_subtitle.Paragraphs.Count-1; i >= 0; i--)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                if (p.Text.Trim().Length == 0)
                {
                }
                else 
                {
                    string text = p.Text.Trim(' ');
                    if (text.StartsWith(Environment.NewLine))
                    {
                        if (AllowFix(i + 1, fixAction1))
                        {
                            p.Text = text.TrimStart(Environment.NewLine.ToCharArray());
                            emptyLinesRemoved++;
                            AddFixToListView(p, i + 1, fixAction1, p.Text, text);
                        }
                    }
                    if (text.EndsWith(Environment.NewLine))
                    {
                        if (AllowFix(i + 1, fixAction2))
                        {
                            p.Text = text.TrimEnd(Environment.NewLine.ToCharArray());
                            emptyLinesRemoved++;
                            AddFixToListView(p, i + 1, fixAction2, p.Text, text);
                        }
                    }
                }
            }

            // this must be the very last action done, or line numbers will be messed up!!!
            for (int i = _subtitle.Paragraphs.Count - 1; i >= 0; i--)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                if (p.Text.Trim().Length == 0)
                {
                    if (AllowFix(i + 1, fixAction0))
                    {
                        _subtitle.Paragraphs.RemoveAt(i);
                        emptyLinesRemoved++;
                        AddFixToListView(p, i + 1, fixAction0, p.Text, string.Format("[{0}]", _language.RemovedEmptyLine));
                    }
                }
            }

            if (emptyLinesRemoved > 0)
            {
                LogStatus(_language.RemovedEmptyLinesUnsedLineBreaks, string.Format(_language.EmptyLinesRemovedX, emptyLinesRemoved));
                _totalFixes += emptyLinesRemoved;
                _subtitle.Renumber(firstNumber);
            }
        }

        public void FixOverlappingDisplayTimes()
        {
            // negative display time
            string fixAction = _language.FixOverlappingDisplayTime;
            int noOfOverlappingDisplayTimesFixed = 0;
            for (int i=0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                Paragraph oldP = new Paragraph(p);
                if (p.Duration.TotalMilliseconds < 0) // negative display time...
                {
                    bool isFixed = false;
                    string status =  string.Format(_language.StartTimeLaterThanEndTime,
                                                    i+1, p.StartTime, p.EndTime, p.Text, Environment.NewLine);

                    Paragraph prev = _subtitle.GetParagraphOrDefault(i - 1);
                    Paragraph next = _subtitle.GetParagraphOrDefault(i + 1);

                    double wantedDisplayTime = Utilities.GetDisplayMillisecondsFromText(p.Text);

                    if (next == null || next.StartTime.TotalMilliseconds > p.StartTime.TotalMilliseconds + wantedDisplayTime)
                    {
                        if (AllowFix(i + 1, fixAction))
                        {
                            p.EndTime.TotalMilliseconds = p.StartTime.TotalMilliseconds + wantedDisplayTime;
                            isFixed = true;
                        }
                    }
                    else if (next.StartTime.TotalMilliseconds > p.StartTime.TotalMilliseconds + 500.0)
                    {
                        if (AllowFix(i + 1, fixAction))
                        {
                            p.EndTime.TotalMilliseconds = p.StartTime.TotalMilliseconds + 500.0;
                            isFixed = true;
                        }
                    }
                    else if (prev == null || next.StartTime.TotalMilliseconds - wantedDisplayTime > prev.EndTime.TotalMilliseconds)
                    {
                        if (AllowFix(i + 1, fixAction))
                        {
                            p.StartTime.TotalMilliseconds = next.StartTime.TotalMilliseconds - wantedDisplayTime;
                            p.EndTime.TotalMilliseconds = next.StartTime.TotalMilliseconds - 1;
                            isFixed = true;
                        }
                    }
                    else
                    {
                        LogStatus(_language.FixOverlappingDisplayTimes, string.Format(_language.UnableToFixStartTimeLaterThanEndTime,
                                                    i + 1, p), true);
                        _totalErrors++;
                    }

                    if (isFixed)
                    {
                        _totalFixes++;
                        noOfOverlappingDisplayTimesFixed++;
                        status = string.Format(_language.XFixedToYZ, status, Environment.NewLine, p);
                        LogStatus(_language.FixOverlappingDisplayTimes, status);
                        AddFixToListView(p, i + 1, fixAction, oldP.ToString(), p.ToString());
                    }
                }
            }

            // overlapping display time             
            for (int i = 1; i < _subtitle.Paragraphs.Count; i++ )
            {
                Paragraph p = _subtitle.Paragraphs[i];
                Paragraph prev = _subtitle.GetParagraphOrDefault(i - 1);
                string oldCurrent = p.ToString();
                string oldPrevious = prev.ToString();
                double prevWantedDisplayTime = Utilities.GetDisplayMillisecondsFromText(prev.Text);
                double currentWantedDisplayTime = Utilities.GetDisplayMillisecondsFromText(p.Text);
                if (prev != null && p.StartTime.TotalMilliseconds <= prev.EndTime.TotalMilliseconds)
                {
                    if (prevWantedDisplayTime <= (p.StartTime.TotalMilliseconds - prev.StartTime.TotalMilliseconds))
                    {
                        if (AllowFix(i + 1, fixAction))
                        {
                            prev.EndTime.TotalMilliseconds = p.StartTime.TotalMilliseconds - 1;
                            _totalFixes++;
                            noOfOverlappingDisplayTimesFixed++;
                            AddFixToListView(p, i + 1, fixAction, oldPrevious, prev.ToString());
                        }
                    }
                    else if (currentWantedDisplayTime <= p.EndTime.TotalMilliseconds - prev.EndTime.TotalMilliseconds)
                    {
                        if (AllowFix(i + 1, fixAction))
                        {
                            p.StartTime.TotalMilliseconds = prev.EndTime.TotalMilliseconds + 1;
                            _totalFixes++;
                            noOfOverlappingDisplayTimesFixed++;
                            AddFixToListView(p, i + 1, fixAction, oldCurrent, p.ToString());
                        }
                    }
                    else if (Math.Abs(p.StartTime.TotalMilliseconds - prev.EndTime.TotalMilliseconds) < 10 && p.Duration.TotalMilliseconds > 1)
                    {
                        if (AllowFix(i + 1, fixAction))
                        {
                            p.StartTime.TotalMilliseconds++;
                            _totalFixes++;
                            noOfOverlappingDisplayTimesFixed++;
                            AddFixToListView(p, i + 1, fixAction, oldCurrent, p.ToString());
                        }
                    }
                    else if (Math.Abs(p.StartTime.TotalMilliseconds - prev.StartTime.TotalMilliseconds) < 10 && Math.Abs(p.EndTime.TotalMilliseconds - prev.EndTime.TotalMilliseconds) < 10)
                    { // merge lines with same time codes
                        if (AllowFix(i + 1, fixAction))
                        {
                            prev.Text = prev.Text.Replace(Environment.NewLine, " ");
                            p.Text = p.Text.Replace(Environment.NewLine, " ");

                            string stripped = Utilities.RemoveHtmlTags(prev.Text).TrimStart();
                            if (!stripped.StartsWith("- "))
                                prev.Text = "- " + prev.Text.TrimStart();

                            stripped = Utilities.RemoveHtmlTags(p.Text).TrimStart();
                            if (!stripped.StartsWith("- "))
                                p.Text = "- " + p.Text.TrimStart();

                            prev.Text = prev.Text.Trim() + Environment.NewLine + p.Text;
                            p.Text = string.Empty; 
                            _totalFixes++;
                            noOfOverlappingDisplayTimesFixed++;
                            AddFixToListView(p, i + 1, fixAction, oldCurrent, p.ToString());

                            p.StartTime.TotalMilliseconds = prev.EndTime.TotalMilliseconds + 1;
                            p.EndTime.TotalMilliseconds = p.StartTime.TotalMilliseconds + 1;
                        }
                    }
                    else
                    {
                        if (AllowFix(i + 1, fixAction))
                        {
                            LogStatus(_language.FixOverlappingDisplayTimes, string.Format(_language.UnableToFixTextXY, i + 1, Environment.NewLine + prev.Number + "  " + prev + Environment.NewLine + p.Number + "  " + p), true);
                            _totalErrors++;
                        }
                        
                    }
                }
            }
            if (noOfOverlappingDisplayTimesFixed > 0)
                LogStatus(fixAction, string.Format(_language.XOverlappingTimestampsFixed, noOfOverlappingDisplayTimesFixed));
        }

        public void FixShortDisplayTimes()
        {
            string fixAction = _language.FixShortDisplayTime;
            int noOfShortDisplayTimes = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                double minDisplayTime = Utilities.GetDisplayMillisecondsFromText(p.Text) * 0.5;
                double displayTime = p.Duration.TotalMilliseconds;
                if (displayTime < minDisplayTime)
                {
                    Paragraph next = _subtitle.GetParagraphOrDefault(i + 1);
                    if (next == null || (p.StartTime.TotalMilliseconds + Utilities.GetDisplayMillisecondsFromText(p.Text)) < next.StartTime.TotalMilliseconds)
                    {
                        if (AllowFix(i + 1, fixAction))
                        {
                            string oldCurrent = p.ToString();
                            p.EndTime.TotalMilliseconds = p.StartTime.TotalMilliseconds + Utilities.GetDisplayMillisecondsFromText(p.Text);
                            _totalFixes++;
                            noOfShortDisplayTimes++;
                            AddFixToListView(p, i + 1, fixAction, oldCurrent, p.ToString());
                        }
                    }
                    else
                    {
                        LogStatus(_language.FixShortDisplayTimes, string.Format(_language.UnableToFixTextXY, i + 1, p));
                        _totalErrors++;
                    }
                }
            }
            if (noOfShortDisplayTimes > 0)
                LogStatus(fixAction, string.Format(_language.XDisplayTimesProlonged, noOfShortDisplayTimes));
        }

        public static int CountTagInText(string text, string tag)
        {
            int count = 0;
            int index = text.IndexOf(tag);
            while (index >= 0)
            {
                count++;
                index = text.IndexOf(tag, index + 1);
            }
            return count;
        }
    
        public void FixInvalidItalicTags()
        {
            const string beginTag = "<i>";
            const string endTag = "</i>";
            string fixAction = _language.FixInvalidItalicTag;
            int noOfInvalidHtmlTags = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                string text = _subtitle.Paragraphs[i].Text.Replace(beginTag.ToUpper(), beginTag).Replace(endTag.ToUpper(), endTag);
                string oldText = text;

                text = text.Replace("< i>", beginTag);
                text = text.Replace("<i >", beginTag);
                text = text.Replace("< I>", beginTag);
                text = text.Replace("<I >", beginTag);

                text = text.Replace("< /i>", endTag);
                text = text.Replace("</ i>", endTag);
                text = text.Replace("< /i>", endTag);
                text = text.Replace("< /I>", endTag);
                text = text.Replace("</ I>", endTag);
                text = text.Replace("< /I>", endTag);

                if (text.Contains(beginTag))
                    text = text.Replace("<i/>", endTag);
                else
                    text = text.Replace("<i/>", string.Empty);

                text = text.Replace(beginTag + beginTag, beginTag);
                text = text.Replace(endTag + endTag, endTag);

                int italicBeginTagCount = CountTagInText(text, beginTag);
                int italicEndTagCount = CountTagInText(text, endTag);
                if (italicBeginTagCount + italicEndTagCount > 0)
                {
                    if (italicBeginTagCount == 1 && italicEndTagCount == 1)
                    {
                        if (text.IndexOf(beginTag) > text.IndexOf(endTag))
                        {
                            text = text.Replace(beginTag, "___________@");
                            text = text.Replace(endTag, beginTag);
                            text = text.Replace("___________@", endTag);
                        }
                    }

                    if (italicBeginTagCount == 2 && italicEndTagCount == 0)
                    {
                        int lastIndex = text.LastIndexOf(beginTag);
                        if (text.Length > lastIndex + endTag.Length)
                            text = text.Substring(0, lastIndex) + endTag + text.Substring(lastIndex -1 + endTag.Length);
                        else
                            text = text.Substring(0, lastIndex) + endTag;
                    }

                    if (italicBeginTagCount == 1 && italicEndTagCount == 2)
                    {
                        int firstIndex = text.IndexOf(endTag);
                        text = text.Substring(0, firstIndex - 1) + text.Substring(firstIndex + endTag.Length);
                    }

                    if (italicBeginTagCount == 2 && italicEndTagCount == 1)
                    {
                        int lastIndex = text.LastIndexOf(beginTag);
                        if (text.Length > lastIndex + endTag.Length)
                            text = text.Substring(0, lastIndex) + text.Substring(lastIndex - 1 + endTag.Length);
                        else
                            text = text.Substring(0, lastIndex - 1) + endTag;
                    }

                    if (italicBeginTagCount == 1 && italicEndTagCount == 0)
                    {
                        if (text.StartsWith(beginTag))
                            text += endTag;
                        else
                            text = text.Replace(beginTag, string.Empty);
                    }

                    if (italicBeginTagCount == 0 && italicEndTagCount == 1)
                    {
                        text = text.Replace(endTag, string.Empty);
                    }

                    text = text.Replace("<i></i>", string.Empty);

                    if (text != oldText)
                    {
                        if (AllowFix(i + 1, fixAction))
                        {
                            _subtitle.Paragraphs[i].Text = text;
                            _totalFixes++;
                            noOfInvalidHtmlTags++;
                            AddFixToListView(_subtitle.Paragraphs[i], i + 1, fixAction, oldText, text);
                        }
                    }
                }
            }
            if (noOfInvalidHtmlTags > 0)
                LogStatus(_language.FixInvalidItalicTags, string.Format(_language.XInvalidHtmlTagsFixed, noOfInvalidHtmlTags));
        }

        public void FixLongDisplayTimes()
        {
            string fixAction = _language.FixLongDisplayTime;
            int noOfLongDisplayTimes = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                double maxDisplayTime = Utilities.GetDisplayMillisecondsFromText(p.Text) * 6.0;
                double displayTime = p.Duration.TotalMilliseconds;
                if (maxDisplayTime < displayTime)
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldCurrent = p.ToString();
                        displayTime = Utilities.GetDisplayMillisecondsFromText(p.Text) * 2.0;
                        p.EndTime.TotalMilliseconds = p.StartTime.TotalMilliseconds + displayTime;
                        _totalFixes++;
                        noOfLongDisplayTimes++;
                        AddFixToListView(p, i + 1, fixAction, oldCurrent, p.ToString());
                    }
                }
            }
            if (noOfLongDisplayTimes > 0)
                LogStatus(_language.FixLongDisplayTimes, string.Format(_language.XDisplayTimesShortned, noOfLongDisplayTimes));
        }

        public void FixLongLines()
        {
            string fixAction = _language.BreakLongLine;
            int noOfLongLines = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                string[] lines = p.Text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                bool tooLong = false;
                foreach (string line in lines)
                {
                    if (line.Length > Configuration.Settings.General.SubtitleLineMaximumLength)
                    {
                        tooLong = true;
                    }
                }
                if (tooLong)
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = Utilities.AutoBreakLine(p.Text);
                        _totalFixes++;
                        noOfLongLines++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
            }
            if (noOfLongLines > 0)
                LogStatus(_language.BreakLongLines, string.Format(_language.XLineBreaksAdded, noOfLongLines));
        }

        public void FixShortLines()
        {
            string fixAction = _language.MergeShortLine;
            int noOfShortLines = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];

                if (p.Text.Length < Configuration.Settings.Tools.MergeLinesShorterThan && p.Text.Contains(Environment.NewLine))
                {
                    string s = p.Text.TrimEnd(".?!:;".ToCharArray());
                    s = s.TrimStart('-');
                    if (!s.Contains(".") &&
                        !s.Contains("?") &&
                        !s.Contains("!") &&
                        !s.Contains(":") &&
                        !s.Contains(";") &&
                        !s.Contains("-") &&
                        p.Text != p.Text.ToUpper()) 
                    {
                        if (AllowFix(i + 1, fixAction))
                        {
                            s = p.Text.Replace(Environment.NewLine, " ");
                            s = s.Replace("  ", " ");

                            string oldCurrent = p.Text;
                            p.Text = s;
                            _totalFixes++;
                            noOfShortLines++;
                            AddFixToListView(p, i + 1, fixAction, oldCurrent, p.Text);
                        }
                    }
                }                
            }
            if (noOfShortLines > 0)
                LogStatus(_language.RemoveLineBreaks, string.Format(_language.XLinesUnbreaked, noOfShortLines));
        }

        public void FixUnneededSpaces()
        {
            string fixAction = _language.UnneededSpace;
            int doubleSpaces = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                string oldText = p.Text;

                while (p.Text.Contains("  "))
                {
                    p.Text = p.Text.Replace("  ", " ");
                }

                if (p.Text.Contains(" " + Environment.NewLine))
                {
                    p.Text = p.Text.Replace(" " + Environment.NewLine, Environment.NewLine);
                }

                if (p.Text.EndsWith(" "))
                {
                    p.Text = p.Text.TrimEnd(' ');
                }

                while (p.Text.Contains(" ,"))
                {
                    p.Text = p.Text.Replace(" ,", ",");
                }

                if (p.Text.EndsWith(" ."))
                {
                    p.Text = p.Text.Substring(0, p.Text.Length - " .".Length) + ".";
                }

                if (p.Text.EndsWith(" \""))
                {
                    p.Text = p.Text.Remove(p.Text.Length - 2, 1);
                }

                if (p.Text.Contains(" \"" + Environment.NewLine))
                {
                    p.Text = p.Text.Replace(" \"" + Environment.NewLine, "\"" + Environment.NewLine);
                }

                if (p.Text.Contains(" ." + Environment.NewLine))
                {
                    p.Text = p.Text.Replace(" ." + Environment.NewLine, "." + Environment.NewLine);
                }

                if (p.Text.EndsWith(" !"))
                {
                    p.Text = p.Text.Substring(0, p.Text.Length - " !".Length) + "!";
                }

                if (p.Text.Contains(" !" + Environment.NewLine))
                {
                    p.Text = p.Text.Replace(" !" + Environment.NewLine, "!" + Environment.NewLine);
                }

                if (p.Text.Contains("! </i>" + Environment.NewLine))
                {
                    p.Text = p.Text.Replace("! </i>" + Environment.NewLine, "!</i>" + Environment.NewLine);
                }

                if (p.Text.Contains(" !</i>" + Environment.NewLine))
                {
                    p.Text = p.Text.Replace(" !</i>" + Environment.NewLine, "!</i>" + Environment.NewLine);
                }

                if (p.Text.EndsWith(" ?</i>"))
                {
                    p.Text = p.Text.Replace(" ?</i>", "?</i>");
                }
                
                if (p.Text.EndsWith(" ?"))
                {
                    p.Text = p.Text.Substring(0, p.Text.Length - " ?".Length) + "?";
                }

                if (p.Text.Contains(" ?" + Environment.NewLine))
                {
                    p.Text = p.Text.Replace(" ?" + Environment.NewLine, "?" + Environment.NewLine);
                }

                if (p.Text.Contains(" ?</i>" + Environment.NewLine))
                {
                    p.Text = p.Text.Replace(" ?</i>" + Environment.NewLine, "?</i>" + Environment.NewLine);
                }

                if (p.Text.Contains("? </i>" + Environment.NewLine))
                {
                    p.Text = p.Text.Replace("? </i>" + Environment.NewLine, "?</i>" + Environment.NewLine);
                }

                if (p.Text.EndsWith(" </i>"))
                {
                    p.Text = p.Text.Substring(0, p.Text.Length - " </i>".Length) + "</i>";
                }
                if (p.Text.Contains(" </i>" + Environment.NewLine))
                {
                    p.Text = p.Text.Replace(" </i>" + Environment.NewLine, "</i>" + Environment.NewLine);
                }
                if (p.Text.EndsWith(" </I>"))
                {
                    p.Text = p.Text.Substring(0, p.Text.Length - " </I>".Length) + "</I>";
                }
                if (p.Text.Contains(" </I>" + Environment.NewLine))
                {
                    p.Text = p.Text.Replace(" </I>" + Environment.NewLine, "</I>" + Environment.NewLine);
                }

                if (p.Text.StartsWith("<i> "))
                {
                    p.Text = "<i>" + p.Text.Substring("<i> ".Length);
                }
                if (p.Text.Contains(Environment.NewLine + "<i> "))
                {
                    p.Text = p.Text.Replace(Environment.NewLine + "<i> ", Environment.NewLine + "<i>");
                }
                if (p.Text.StartsWith("<I> "))
                {
                    p.Text = "<I>" + p.Text.Substring("<I> ".Length);
                }
                if (p.Text.Contains(Environment.NewLine + "<I> "))
                {
                    p.Text = p.Text.Replace(Environment.NewLine + "<I> ", Environment.NewLine + "<I>");
                }

                if (p.Text != oldText)
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        doubleSpaces++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }


            }
            if (doubleSpaces > 0)
                LogStatus(_language.RemoveUnneededSpaces, string.Format(_language.XUnneededSpacesRemoved, doubleSpaces));
        }

        public void FixUnneededPeriods()
        {
            string fixAction = _language.UnneededPeriod;
            int unneededPeriods = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                if (p.Text.Contains("!." + Environment.NewLine))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.Replace("!." + Environment.NewLine, "!" + Environment.NewLine);
                        unneededPeriods++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
                if (p.Text.Contains("?." + Environment.NewLine))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.Replace("?." + Environment.NewLine, "?" + Environment.NewLine);
                        unneededPeriods++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
                if (p.Text.EndsWith("!."))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.TrimEnd('.');
                        unneededPeriods++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
                if (p.Text.EndsWith("?."))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.TrimEnd('.');
                        unneededPeriods++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }

                if (p.Text.Contains("!. "))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.Replace("!. ", "! ");
                        unneededPeriods++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
                if (p.Text.Contains("?. "))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.Replace("?. ", "? ");
                        unneededPeriods++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }

            }
            if (unneededPeriods > 0)
                LogStatus(_language.RemoveUnneededPeriods, string.Format(_language.XUnneededPeriodsRemoved, unneededPeriods));
        }

        public void FixMissingSpaces()
        {
            string fixAction = _language.FixMissingSpace;
            Regex reComma = new Regex(@"[^\s\d],[^\s]", RegexOptions.Compiled);
            Regex rePeriod = new Regex(@"[a-z][.][a-zA-Z]", RegexOptions.Compiled);
            Regex reQuestionMark = new Regex(@"[^\s\d]\?[a-zA-Z]", RegexOptions.Compiled);
            Regex reExclamation = new Regex(@"[^\s\d]\![a-zA-Z]", RegexOptions.Compiled);
            Regex reColon = new Regex(@"[^\s\d]\:[a-zA-Z]", RegexOptions.Compiled);          

            Regex urlCom = new Regex(@"\w\.com\b", RegexOptions.Compiled);
            Regex urlNet = new Regex(@"\w\.net\b", RegexOptions.Compiled);
            Regex urlOrg = new Regex(@"\w\.org\b", RegexOptions.Compiled);

            int missingSpaces = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];

                // missing space after comma ","
                Match match = reComma.Match(p.Text);
                if (match.Success)
                {
                    while (match.Success)
                    {
                        if ("\"<".Contains(p.Text[match.Index + 2].ToString()) == false)
                        {
                            if (AllowFix(i + 1, fixAction))
                            {
                                _totalFixes++;
                                missingSpaces++;

                                string oldText = p.Text;
                                p.Text = p.Text.Replace(match.Value, match.Value[0] + ", " + match.Value[match.Value.Length - 1]);
                                AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                            }
                        }
                        match = match.NextMatch();
                    }
                }

                // missing space after "?"
                match = reQuestionMark.Match(p.Text);
                if (match.Success)
                {
                    while (match.Success)
                    {
                        if ("\"<".Contains(p.Text[match.Index + 2].ToString()) == false)
                        {
                            if (AllowFix(i + 1, fixAction))
                            {
                                _totalFixes++;
                                missingSpaces++;

                                string oldText = p.Text;
                                p.Text = p.Text.Replace(match.Value, match.Value[0] + "? " + match.Value[match.Value.Length - 1]);
                                AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                            }
                        }
                        match = reQuestionMark.Match(p.Text, match.Index + 1);
                    }
                }

                // missing space after "!"
                match = reExclamation.Match(p.Text);
                if (match.Success)
                {
                    while (match.Success)
                    {
                        if ("\"<".Contains(p.Text[match.Index + 2].ToString()) == false)
                        {
                            if (AllowFix(i + 1, fixAction))
                            {
                                _totalFixes++;
                                missingSpaces++;

                                string oldText = p.Text;
                                p.Text = p.Text.Replace(match.Value, match.Value[0] + "! " + match.Value[match.Value.Length - 1]);
                                AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                            }
                        }
                        match = reExclamation.Match(p.Text, match.Index + 1);
                    }
                }

                // missing space after ":"
                match = reColon.Match(p.Text);
                if (match.Success)
                {
                    while (match.Success)
                    {
                        if ("\"<".Contains(p.Text[match.Index + 2].ToString()) == false)
                        {
                            if (AllowFix(i + 1, fixAction))
                            {
                                _totalFixes++;
                                missingSpaces++;

                                string oldText = p.Text;
                                p.Text = p.Text.Replace(match.Value, match.Value[0] + ": " + match.Value[match.Value.Length - 1]);
                                AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                            }
                        }
                        match = reColon.Match(p.Text, match.Index + 1);
                    }
                }

                // missing space after period "."
                match = rePeriod.Match(p.Text);
                if (match.Success)
                {
                    while (match.Success)
                    {
                        if (!p.Text.ToLower().Contains("www.") && 
                            !p.Text.ToLower().Contains("http://") &&
                            !urlCom.IsMatch(p.Text) &&
                            !urlNet.IsMatch(p.Text) &&
                            !urlOrg.IsMatch(p.Text)) // urls are skipped
                        {
                            bool isMatchAbbreviation = false;

                            string word = GetWordFromIndex(p.Text, match.Index);
                            word = RemoveEverySecondLetter(word, 1);
                            if (!word.Contains("."))
                                isMatchAbbreviation = true;

                            if (match.Value.ToLower() == "h.d" && match.Index > 0 && p.Text.Substring(match.Index - 1, 4).ToLower() == "ph.d")
                                isMatchAbbreviation = true;

                            if (!isMatchAbbreviation && AllowFix(i + 1, fixAction))
                            {
                                _totalFixes++;
                                missingSpaces++;

                                string oldText = p.Text;
                                p.Text = p.Text.Replace(match.Value, match.Value[0] + ". " + match.Value[match.Value.Length - 1]);
                                AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                            }
                        }
                        match = match.NextMatch();
                    }
                }
            }
            if (missingSpaces > 0)
                LogStatus(_language.FixMissingSpaces, string.Format(_language.XMissingSpacesAdded, missingSpaces));
        }

        private static string RemoveEverySecondLetter(string text, int start)
        {
            int i = start;
            while (i < text.Length)
            {
                text = text.Remove(i, 1);
                i++;
            }
            return text;
        }

        private static string GetWordFromIndex(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
                return string.Empty;

            int endIndex = index;
            for (int i=index; i<text.Length; i++)
            {
                if ((" " + Environment.NewLine).Contains(text[i].ToString()))
                    break;
                endIndex = i;
            }

            int startIndex = index;
            for (int i = index; i >= 0; i--)
            {
                if ((" " + Environment.NewLine).Contains(text[i].ToString()))
                    break;
                startIndex = i;
            }

            string s = text.Substring(startIndex, endIndex - startIndex + 1);
            return s;
        }

        public void AddMissingQuotes()
        {
            string fixAction = _language.AddMissingQuote;
            int noOfFixes = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];                
                if (CountTagInText(p.Text, "\"") == 1)
                {
                    string oldText = p.Text;
                    if (p.Text.StartsWith("\""))
                    {
                        Paragraph next = _subtitle.GetParagraphOrDefault(i + 1);
                        if (next == null || !next.Text.Contains("\""))
                        { 
                            p.Text += "\"";
                        }
                    }
                    else if (p.Text.EndsWith("\""))
                    {
                        Paragraph prev = _subtitle.GetParagraphOrDefault(i - 1);
                        if (prev == null || !prev.Text.Contains("\""))
                        {
                            p.Text = "\"" + p.Text;
                        }
                    }

                    if (oldText != p.Text)
                    {
                        if (AllowFix(i + 1, fixAction))
                        {
                                _totalFixes++;
                                noOfFixes++;
                                AddFixToListView(p, i + 1, fixAction, oldText, p.Text);                            
                        }
                    }
                }
            }
            if (noOfFixes > 0)
                LogStatus(fixAction, string.Format(_language.XMissingQuotesAdded, noOfFixes));
        }

        private static string GetWholeWord(string text, int index)
        {
            int start = index;
            while (start > 0 && (Environment.NewLine + " ,.!?\"'=()/-").Contains(text[start - 1].ToString()) == false)
                start--;

            int end = index;
            while (end+1 < text.Length && (Environment.NewLine + " ,.!?\"'=()/-").Contains(text[end+1].ToString()) == false)
                end++;

            return text.Substring(start, end - start +1);
        }

        public void FixUppercaseIInsideWords()
        {
            string fixAction = _language.FixUppercaseIInsideLowercaseWord;
            int uppercaseIsInsideLowercaseWords = 0;
            Regex reAfterLowercaseLetter = new Regex(@"[a-zæøåäöé]I", RegexOptions.Compiled);
            Regex reBeforeLowercaseLetter = new Regex(@"I[a-zæøåäöé]", RegexOptions.Compiled);
            bool isLineContinuation = false;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                string oldText = p.Text;

                Match match = reAfterLowercaseLetter.Match(p.Text);
                if (match.Success)
                {
                    while (match.Success)
                    {
                        if (!(match.Index > 1 && p.Text.Substring(match.Index - 1, 2) == "Mc")) // irish names, McDonalds etc.
                        {
                            if (p.Text[match.Index + 1] == 'I')
                            {
                                if (AllowFix(i + 1, fixAction))
                                {
                                    p.Text = p.Text.Substring(0, match.Index + 1) + "l";
                                    if (match.Index + 2 < oldText.Length)
                                        p.Text += oldText.Substring(match.Index + 2);

                                    uppercaseIsInsideLowercaseWords++;
                                    _totalFixes++;
                                    AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                                }
                            }
                        }
                        match = match.NextMatch();
                    }
                }

                StripableText st = new StripableText(p.Text);
                match = reBeforeLowercaseLetter.Match(st.StrippedText);
                if (match.Success)
                {
                    while (match.Success)
                    {
                        string word = GetWholeWord(st.StrippedText, match.Index);
                        if (!IsName(word))
                        {
                            if (AllowFix(i + 1, fixAction))
                            {
                                if (word.ToLower() == "internal" ||
                                    word.ToLower() == "island" ||
                                    word.ToLower() == "islands")
                                {
                                }
                                else if (match.Index == 0)
                                {  // first letter in paragraph

                                    //too risky! - perhaps if periods is fixed at the same time... or too complicated!?
                                    //if (isLineContinuation)
                                    //{
                                    //    st.StrippedText = st.StrippedText.Remove(match.Index, 1).Insert(match.Index, "l");
                                    //    p.Text = st.MergedString;
                                    //    uppercaseIsInsideLowercaseWords++;
                                    //    _totalFixes++;
                                    //    AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                                    //}
                                }
                                else
                                {
                                    if (match.Index > 2 && st.StrippedText[match.Index - 1] == ' ')
                                    {
                                        if ((Utilities.GetLetters(true, true, true) + ",").Contains(st.StrippedText[match.Index - 2].ToString()))
                                        {
                                            st.StrippedText = st.StrippedText.Remove(match.Index, 1).Insert(match.Index, "l");
                                            p.Text = st.MergedString;
                                            uppercaseIsInsideLowercaseWords++;
                                            _totalFixes++;
                                            AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                                        }
                                    }
                                    else if (match.Index > Environment.NewLine.Length + 1 && Environment.NewLine.Contains(st.StrippedText[match.Index - 1].ToString()))
                                    {
                                        if ((Utilities.GetLetters(true, true, true) + ",").Contains(st.StrippedText[match.Index - (Environment.NewLine.Length + 1)].ToString()))
                                        {
                                            st.StrippedText = st.StrippedText.Remove(match.Index, 1).Insert(match.Index, "l");
                                            p.Text = st.MergedString;
                                            uppercaseIsInsideLowercaseWords++;
                                            _totalFixes++;
                                            AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                                        }
                                    }
                                    else if (match.Index > 1 && ((st.StrippedText[match.Index - 1] == '\"') || (st.StrippedText[match.Index - 1] == '>')))
                                    {
                                    }
                                    else 
                                    {
                                        st.StrippedText = st.StrippedText.Remove(match.Index, 1).Insert(match.Index, "l");
                                        p.Text = st.MergedString;
                                        uppercaseIsInsideLowercaseWords++;
                                        _totalFixes++;
                                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                                    }
                                }

                            }
                        }
                        match = match.NextMatch();
                    }
                }

                isLineContinuation = p.Text.Length > 0 && Utilities.GetLetters(true, true, false).Contains(p.Text[p.Text.Length - 1].ToString());
            }
            if (uppercaseIsInsideLowercaseWords > 0)
                LogStatus(_language.FixUppercaseIInsindeLowercaseWords, string.Format(_language.XUppercaseIsFoundInsideLowercaseWords, uppercaseIsInsideLowercaseWords));
        }
        
        public void FixDoubleApostrophes()
        {
            string fixAction = _language.FixDoubleApostrophes;
            int fixCount = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];

                if (p.Text.Contains("''"))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.Replace("''", "\"");
                        fixCount++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
            }
            if (fixCount > 0)
                LogStatus(_language.FixDoubleApostrophes, string.Format(_language.XDoubleApostrophesFixed, fixCount));
        }

        public void FixMissingPeriodsAtEndOfLine()
        {
            string fixAction = _language.FixMissingPeriodAtEndOfLine;
            int missigPeriodsAtEndOfLine = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                Paragraph next = _subtitle.GetParagraphOrDefault(i + 1);

                if (next != null &&
                    next.Text.Length > 0 &&
                    Utilities.GetLetters(true, false, false).Contains(next.Text[0].ToString()) &&
                    p.Text.Length > 0 &&
                    (!"\",.!?:;>-])♪♫".Contains(p.Text[p.Text.Length - 1].ToString())))
                {
                    if (!p.Text.EndsWith(")") && !p.Text.EndsWith("]")) // hear impaired
                    {
                        if (p.Text != p.Text.ToUpper())
                        {
                            //don't end the sentence if the next word is an I word as they're always capped.
                            if (!next.Text.StartsWith("I ") && !next.Text.StartsWith("I'"))
                            {
                                //test to see if the first word of the next line is a name
                                if (!IsName(next.Text.Split(" .,-?!:;\"()[]{}|<>/+\r\n".ToCharArray())[0]))
                                {
                                    if (AllowFix(i + 1, fixAction))
                                    {
                                        string oldText = p.Text;
                                        _totalFixes++;
                                        missigPeriodsAtEndOfLine++;
                                        p.Text += ".";
                                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (next != null && !string.IsNullOrEmpty(p.Text) && Utilities.GetLetters(true, true, true).Contains(p.Text[p.Text.Length-1].ToString()))
                {
                    if (p.Text != p.Text.ToUpper())
                    {
                        StripableText st = new StripableText(next.Text);
                        if (st.StrippedText.Length > 0 && st.StrippedText != st.StrippedText.ToUpper() &&
                            Utilities.GetLetters(true, false, false).Contains(st.StrippedText[0].ToString()))
                        {
                            if (AllowFix(i + 1, fixAction))
                            {
                                int j = p.Text.Length - 1;
                                while (j >= 0 && !(".!?¿¡").Contains(p.Text[j].ToString()))
                                    j--;
                                string endSign = ".";
                                if (j >= 0 && p.Text[j] == '¿')
                                    endSign = "?";
                                if (j >= 0 && p.Text[j] == '¡')
                                    endSign = "!";

                                string oldText = p.Text;
                                _totalFixes++;
                                missigPeriodsAtEndOfLine++;
                                p.Text += endSign;
                                AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                            }
                        }
                    }
                }

                if (p.Text.Length > 4)
                {
                    int indexOfNewLine = p.Text.IndexOf(Environment.NewLine + " -", 3);
                    if (indexOfNewLine == -1)
                        indexOfNewLine = p.Text.IndexOf(Environment.NewLine + "-", 3);
                    if (indexOfNewLine == -1)
                        indexOfNewLine = p.Text.IndexOf(Environment.NewLine + "<i>-", 3);
                    if (indexOfNewLine == -1)
                        indexOfNewLine = p.Text.IndexOf(Environment.NewLine + "<i> -", 3);
                    if (indexOfNewLine > 0)
                    {
                        if (Configuration.Settings.General.UppercaseLetters.Contains(p.Text[indexOfNewLine - 1].ToString().ToUpper()))
                        {
                            if (AllowFix(i + 1, fixAction))
                            {
                                string oldText = p.Text;

                                string text = p.Text.Substring(0, indexOfNewLine);
                                StripableText st = new StripableText(text);
                                if (st.Pre.TrimEnd().EndsWith("¿")) // Spanish ¿
                                    p.Text = p.Text.Insert(indexOfNewLine, "?");
                                else if (st.Pre.TrimEnd().EndsWith("¡")) // Spanish ¡
                                    p.Text = p.Text.Insert(indexOfNewLine, "!");
                                else
                                    p.Text = p.Text.Insert(indexOfNewLine, ".");

                                _totalFixes++;
                                missigPeriodsAtEndOfLine++;
                                AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                            }
                        }
                    }
                }
            }

            if (missigPeriodsAtEndOfLine > 0)
                LogStatus(_language.AddPeriods, string.Format(_language.XPeriodsAdded, missigPeriodsAtEndOfLine));
        }

        private bool IsName(string candidate)
        {
            MakeSureNamesListIsLoaded();
            return _namesEtcList.Contains(candidate);
        }

        private void MakeSureNamesListIsLoaded()
        {
            if (_namesEtcList == null)
            {
                _namesEtcList = new List<string>();
                string languageTwoLetterCode = Utilities.AutoDetectGoogleLanguage(_subtitle);

                // Will contains both one word names and multi names
                Utilities.LoadNamesEtcWordLists(_namesEtcList, _namesEtcList, languageTwoLetterCode);
            }
        }

        private List<string> GetAbbreviations()
        {
            if (_abbreviationList != null)
                return _abbreviationList;

            MakeSureNamesListIsLoaded();
            _abbreviationList = new List<string>();
            foreach (string name in _namesEtcList)
            {
                if (name.EndsWith("."))
                    _abbreviationList.Add(name);
            }
            return _abbreviationList;
        }

        private void FixStartWithUppercaseLetterAfterParagraph()
        {
            string fixAction1 = _language.FixFirstLetterToUppercaseAfterParagraph + " ";
            string fixAction2 = _language.FixFirstLetterToUppercaseAfterParagraph;
            int fixedStartWithUppercaseLetterAfterParagraphTicked = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                Paragraph prev = _subtitle.GetParagraphOrDefault(i - 1);

                if (p.Text != null && p.Text.Length > 1)
                {
                    string text = p.Text;
                    string pre = string.Empty;
                    if (text.Length > 4 && text.StartsWith("<i> "))
                    {
                        pre = "<i> ";
                        text = text.Substring(4);
                    }
                    if (text.Length > 3 && text.StartsWith("<i>"))
                    {
                        pre = "<i>";
                        text = text.Substring(3);
                    }
                    if (text.Length > 4 && text.StartsWith("<I> "))
                    {
                        pre = "<I> ";
                        text = text.Substring(4);
                    }
                    if (text.Length > 3 && text.StartsWith("<I>"))
                    {
                        pre = "<I>";
                        text = text.Substring(3);
                    }

                    string oldText = p.Text;
                    string firstLetter = text.Substring(0, 1);

                    string prevText = " .";
                    if (prev != null)
                        prevText = Utilities.RemoveHtmlTags(prev.Text);

                    if (!text.StartsWith("www.") &&
                        firstLetter != firstLetter.ToUpper() &&
                        !"0123456789".Contains(firstLetter) && 
                        prevText.Length > 1 &&
                        !prevText.EndsWith("...") &&
                        (prevText.EndsWith(".") ||
                         prevText.EndsWith("!") ||
                         prevText.EndsWith("?") ||
                         prevText.EndsWith(":") ||
                         prevText.EndsWith(";")))
                    {
                        bool isMatchInKnowAbbreviations = _autoDetectGoogleLanguage == "en" &&
                            (prevText.EndsWith(" o.r.") ||
                             prevText.EndsWith(" a.m.") ||
                             prevText.EndsWith(" p.m."));

                        if (!isMatchInKnowAbbreviations && AllowFix(i + 1, fixAction1))
                        {
                            p.Text = pre + firstLetter.ToUpper() + text.Substring(1);
                            _totalFixes++;
                            fixedStartWithUppercaseLetterAfterParagraphTicked++;
                            AddFixToListView(p, i + 1, fixAction1, oldText, p.Text);
                        }
                    }
                }

                if (p.Text != null && p.Text.Contains(Environment.NewLine))
                {
                    string[] arr = p.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    if (arr.Length == 2 && arr[1].Length > 1)
                    {
                        string text = arr[1];
                        string pre = string.Empty;
                        if (text.Length > 4 && text.StartsWith("<i> "))
                        {
                            pre = "<i> ";
                            text = text.Substring(4);
                        }
                        if (text.Length > 3 && text.StartsWith("<i>"))
                        {
                            pre = "<i>";
                            text = text.Substring(3);
                        }
                        if (text.Length > 4 && text.StartsWith("<I> "))
                        {
                            pre = "<I> ";
                            text = text.Substring(4);
                        }
                        if (text.Length > 3 && text.StartsWith("<I>"))
                        {
                            pre = "<I>";
                            text = text.Substring(3);
                        }

                        string oldText = p.Text;
                        string firstLetter = text.Substring(0, 1);
                        string prevText = Utilities.RemoveHtmlTags(arr[0]);

                        if (!text.StartsWith("www.") &&
                            firstLetter != firstLetter.ToUpper() &&
                            !prevText.EndsWith("...") &&
                            prevText.Length > 1 &&
                            !"0123456789".Contains(firstLetter) && 
                            (prevText.EndsWith(".") ||
                             prevText.EndsWith("!") ||
                             prevText.EndsWith("?") ||
                             prevText.EndsWith(":") ||
                             prevText.EndsWith(";")))
                        {
                            bool isMatchInKnowAbbreviations = _autoDetectGoogleLanguage == "en" &&
                                (prevText.EndsWith(" o.r.") ||
                                 prevText.EndsWith(" a.m.") ||
                                 prevText.EndsWith(" p.m."));


                            if (!isMatchInKnowAbbreviations && AllowFix(i + 1, fixAction2))
                            {
                                text = pre + firstLetter.ToUpper() + text.Substring(1);
                                _totalFixes++;
                                fixedStartWithUppercaseLetterAfterParagraphTicked++;
                                p.Text = arr[0] + Environment.NewLine + text;
                                AddFixToListView(p, i + 1, fixAction2, oldText, p.Text);
                            }
                        }
                    }
                }

                if (p.Text.Length > 4)
                {
                    int indexOfNewLine = p.Text.IndexOf(Environment.NewLine + " -", 1);
                    if (indexOfNewLine == -1)
                        indexOfNewLine = p.Text.IndexOf(Environment.NewLine + "-", 1);
                    if (indexOfNewLine == -1)
                        indexOfNewLine = p.Text.IndexOf(Environment.NewLine + "<i>-", 1);
                    if (indexOfNewLine == -1)
                        indexOfNewLine = p.Text.IndexOf(Environment.NewLine + "<i> -", 1);
                    if (indexOfNewLine > 0)
                    {
                        string text = p.Text.Substring(indexOfNewLine + 2);
                        StripableText st = new StripableText(text);
                        if (st.StrippedText.Length > 0 && st.StrippedText[0].ToString() != st.StrippedText[0].ToString().ToUpper())
                        {
                            text = st.Pre + st.StrippedText.Remove(0, 1).Insert(0, st.StrippedText[0].ToString().ToUpper()) + st.Post;

                            if (AllowFix(i + 1, fixAction2))
                            {
                                string oldText = p.Text;
                                p.Text = p.Text.Remove(indexOfNewLine + 2).Insert(indexOfNewLine + 2, text);
                                _totalFixes++;
                                fixedStartWithUppercaseLetterAfterParagraphTicked++;
                                AddFixToListView(p, i + 1, fixAction2, oldText, p.Text);
                            }

                        }
                    }

                }
            }
            if (fixedStartWithUppercaseLetterAfterParagraphTicked > 0)
                LogStatus(_language.StartWithUppercaseLetterAfterParagraph, fixedStartWithUppercaseLetterAfterParagraphTicked + " periods added.");
        }

        private void FixStartWithUppercaseLetterAfterPeriodInsideParagraph()
        {
            string fixAction = _language.StartWithUppercaseLetterAfterPeriodInsideParagraph;
            int noOfFixes = 0;
            string lastLine = string.Empty;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                string oldText = p.Text;
                StripableText st = new StripableText(p.Text);
                if (p.Text.Length > 3)
                {
                    string text = st.StrippedText.Replace("  ", " ");
                    int start = text.IndexOfAny(".!?".ToCharArray());
                    while (start != -1 && start < text.Length)
                    {
                        if (start > 0 && Utilities.IsInteger(text[start - 1].ToString()))
                        { 
                            // ignore periods after a number
                        }
                        else if (start + 4 < text.Length && text[start + 1] == ' ')
                        {
                            if (!IsAbbreviation(text, start))
                            {
                                StripableText subText = new StripableText(text.Substring(start + 2));
                                if (subText.StrippedText.Length > 0 && Configuration.Settings.General.UppercaseLetters.ToLower().Contains(subText.StrippedText[0].ToString()))
                                {
                                    if (subText.StrippedText.Length > 1)
                                    {
                                        text = text.Substring(0, start + 2) + subText.Pre + subText.StrippedText[0].ToString().ToUpper() + subText.StrippedText.Substring(1) + subText.Post;
                                        if (AllowFix(i + 1, fixAction))
                                        {
                                            p.Text = st.Pre + text + st.Post;
                                        }
                                    }
                                }
                            }
                        }
                        start+=4;
                        if (start < text.Length)
                            start = text.IndexOfAny(".!?".ToCharArray(), start);
                    }
                }

                if (oldText != p.Text)
                {
                    noOfFixes++;
                    _totalFixes++;
                    AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                }
            }
            if (noOfFixes > 0)
                LogStatus(_language.StartWithUppercaseLetterAfterPeriodInsideParagraph, noOfFixes.ToString());

        }

        private bool IsAbbreviation(string text, int index)
        {
            if (text[index] != '.' && text[index] != '!' && text[index] != '?')
                return false;

            if (index - 3 > 0 && text[index - 1] != '.' && text[index - 2] == '.') // e.g: O.R.
                return false;

            string word = string.Empty;
            int i = index - 1;
            while (i >= 0 && Utilities.GetLetters(true, true, false).Contains(text[i].ToString()))
            {
                word = text[i].ToString() + word;
                i--;
            }

            List<string> abbreviations = GetAbbreviations();
            return abbreviations.Contains(word + ".");
        }

        private void FixOcrErrorsViaReplaceList(string threeLetterISOLanguageName)
        {
            OcrFixEngine ocrFixEngine = new OcrFixEngine(threeLetterISOLanguageName, this);
            string fixAction = _language.FixCommonOcrErrors;
            int noOfFixes = 0;
            string lastLine = string.Empty;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                string text = ocrFixEngine.FixOcrErrors(p.Text, i, lastLine, false, true);
                lastLine = text;
                if (p.Text != text)
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = text;
                        noOfFixes++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
            }
            if (noOfFixes > 0)
                LogStatus(_language.FixCommonOcrErrors, string.Format(_language.CommonOcrErrorsFixed, noOfFixes));
        }

        private void FixAloneLowercaseIToUppercaseI()
        {
            string fixAction = _language.FixLowercaseIToUppercaseI;
            int iFixes = 0;
            var re = new Regex(@"\bi\b", RegexOptions.Compiled);
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];

                string oldText = p.Text;
                string s = p.Text;
                if (s.Contains("i"))
                {
                    //html tags
                    if (s.Contains(">i</"))
                        s = s.Replace(">i</", ">I</");
                    if (s.Contains(">i "))
                        s = s.Replace(">i ", ">I ");
                    if (s.Contains(">i" + Environment.NewLine))
                        s = s.Replace(">i" + Environment.NewLine, ">I" + Environment.NewLine);

                    // reg-ex
                    Match match = re.Match(s);
                    if (match.Success)
                    {
                        while (match.Success)
                        {
                            if (s[match.Index] == 'i')
                            {
                                string prev = string.Empty;
                                string next = string.Empty;
                                if (match.Index > 0)
                                    prev = s[match.Index - 1].ToString();
                                if (match.Index + 1 < s.Length)
                                    next = s[match.Index + 1].ToString();
                                if (prev != ">" && next != ">")
                                {
                                    string temp = s.Substring(0, match.Index) + "I";
                                    if (match.Index + 1 < oldText.Length)
                                        temp += s.Substring(match.Index + 1);
                                    s = temp;
                                }
                            }
                            match = match.NextMatch();
                        }
                    }

                    if (s != oldText && AllowFix(i + 1, fixAction))
                    {
                        p.Text = s;
                        iFixes++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }

                }
            }
            if (iFixes > 0)
                LogStatus(_language.FixLowercaseIToUppercaseI, string.Format(_language.XIsChangedToUppercase, iFixes));
        }

        private void FixHyphens()
        {
            string fixAction = _language.FixHyphen;
            int iFixes = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                string text = p.Text;

                if (text.Trim().StartsWith("-") ||
                    text.Trim().StartsWith("<i>-") ||
                    text.Trim().StartsWith("<i> -") ||
                    text.Trim().StartsWith("<I>-") ||
                    text.Trim().StartsWith("<I> -") ||
                    text.Contains(Environment.NewLine + "-") ||
                    text.Contains(Environment.NewLine + " -") ||
                    text.Contains(Environment.NewLine + "<i>-") ||
                    text.Contains(Environment.NewLine + "<i> -") ||
                    text.Contains(Environment.NewLine + "<I>-") ||
                    text.Contains(Environment.NewLine + "<I> -"))
                {
                    Paragraph prev = _subtitle.GetParagraphOrDefault(i - 1);
                    
                    if (prev == null || !Utilities.RemoveHtmlTags(prev.Text).Trim().EndsWith("-"))
                    {
                        if (CountTagInText(text, "-") == 1)
                        {
                            string oldText = p.Text;

                            text = text.Replace(" - ", string.Empty);
                            text = text.Replace(" -", string.Empty);
                            text = text.Replace("- ", string.Empty);
                            text = text.Replace("-", string.Empty);
                            if (text != oldText)
                            {
                                if (AllowFix(i + 1, fixAction))
                                {
                                    p.Text = text;
                                    iFixes++;
                                    _totalFixes++;
                                    AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                                }
                            }
                        }
                    }
                }

            }
            if (iFixes > 0)
                LogStatus(_language.FixHyphens, string.Format(_language.XHyphensFixed, iFixes));
        }

        private void Fix3PlusLines()
        {
            string fixAction = _language.Fix3PlusLine;
            int iFixes = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                string text = p.Text;

                if (CountTagInText(text, Environment.NewLine) > 1)
                {
                    string oldText = p.Text;
                    text = Utilities.AutoBreakLine(text);

                    if (AllowFix(i + 1, fixAction))
                    {
                        p.Text = text;
                        iFixes++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
            }
            if (iFixes > 0)
                LogStatus(_language.Fix3PlusLines, string.Format(_language.X3PlusLinesFixed, iFixes));
        }

        public void FixMusicNotation()
        {
            string fixAction = _language.FixMusicNotation;
            int fixCount = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];

                string[] musicSymbols = Configuration.Settings.Tools.MusicSymbolToReplace.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                string oldText = p.Text;

                foreach (string musicSymbol in musicSymbols)
                {
                    p.Text = p.Text.Replace(musicSymbol, Configuration.Settings.Tools.MusicSymbol);
                    p.Text = p.Text.Replace(musicSymbol.ToUpper(), Configuration.Settings.Tools.MusicSymbol);
                }

                if (!p.Text.Equals(oldText))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        fixCount++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
            }
            if (fixCount > 0)
                LogStatus(_language.FixMusicNotation, string.Format(_language.XFixMusicNotation, fixCount));
        }

        public void FixDoubleDash()
        {
            string fixAction = _language.FixDoubleDash;
            int fixCount = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];

                if (p.Text.Contains("--"))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.Replace("--", "... ");
                        p.Text = p.Text.Replace("...  ", "... ");
                        p.Text = p.Text.Replace(" ...", "...");
                        
                        fixCount++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
                if (p.Text.EndsWith("-"))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.Substring(0, p.Text.Length - 1) + "...";
                        p.Text = p.Text.Replace(" ...", "...");
                        fixCount++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
            }
            if (fixCount > 0)
                LogStatus(_language.FixDoubleDash, string.Format(_language.XFixDoubleDash, fixCount));
        }

        public void FixDoubleGreaterThan()
        {
            string fixAction = _language.FixDoubleGreaterThan;
            int fixCount = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];

                if (p.Text.StartsWith(">> "))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.Substring(3, p.Text.Length - 3);
                        fixCount++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
                if (p.Text.StartsWith(">>"))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.Substring(2, p.Text.Length - 2);
                        fixCount++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
            }
            if (fixCount > 0)
                LogStatus(_language.FixDoubleGreaterThan, string.Format(_language.XFixDoubleGreaterThan, fixCount));
        }

        public void FixEllipsesStart()
        {
            string fixAction = _language.FixEllipsesStart;
            int fixCount = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];

                if (p.Text.StartsWith("..."))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.Substring(3, p.Text.Length - 3);
                        fixCount++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }

                if (p.Text.Contains(": ..."))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        p.Text = p.Text.Replace(": ...", ": ");
                        fixCount++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }

            }
            if (fixCount > 0)
                LogStatus(_language.FixEllipsesStart, string.Format(_language.XFixEllipsesStart, fixCount));
        }

        public void FixMissingOpenBracket()
        {
            string fixAction = _language.FixMissingOpenBracket;
            int fixCount = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];

                if (p.Text.Contains("]") && !p.Text.Contains("["))
                {
                    if (AllowFix(i + 1, fixAction))
                    {
                        string oldText = p.Text;
                        string pre = "";
                        string oBkt = "[";
                        if (p.Text.Contains(" ]"))
                            oBkt = "[ ";

                        if (p.Text.Length > 3 && p.Text.StartsWith("<i>"))
                        {
                            pre = "<i>";
                            p.Text = p.Text.Substring(3);
                        }
                        p.Text = pre + oBkt + p.Text;
                        fixCount++;
                        _totalFixes++;
                        AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                    }
                }
            }
            if (fixCount > 0)
                LogStatus(_language.FixMissingOpenBracket, string.Format(_language.XFixMissingOpenBracket, fixCount));
        }

        private static Regex MyRegEx(string inputRegex)
        {
            return new Regex(inputRegex.Replace(" ", "[ \r\n]+"), RegexOptions.Compiled);
        }

        private void FixDanishLetterI()
        {
            const string fixAction = "Fix danish letter 'i'";
            int fixCount = 0;

            var littleIRegex = new Regex(@"\bi\b", RegexOptions.Compiled);

            var iList = new List<Regex>
                             { // not a complete list, more phrases will come
                                 MyRegEx(@", i ved nok\b"),
                                 MyRegEx(@", i ved, "),
                                 MyRegEx(@", i ved."),
                                 MyRegEx(@", i ikke blev\b"),                                 
                                 MyRegEx(@"\b i føler at\b"),                                 
                                 MyRegEx(@"\badvarede i os\b"), 
                                 MyRegEx(@"\badvarede i dem\b"), 
                                 MyRegEx(@"\bat i aldrig\b"), 
                                 MyRegEx(@"\bat i alle bliver\b"), 
                                 MyRegEx(@"\bat i alle er\b"), 
                                 MyRegEx(@"\bat i alle forventer\b"), 
                                 MyRegEx(@"\bat i alle gør\b"), 
                                 MyRegEx(@"\bat i alle har\b"), 
                                 MyRegEx(@"\bat i alle ved\b"), 
                                 MyRegEx(@"\bat i alle vil\b"), 
                                 MyRegEx(@"\bat i bare\b"),
                                 MyRegEx(@"\bat i bager\b"),
                                 MyRegEx(@"\bat i bruger\b"),
                                 MyRegEx(@"\bat i dræber\b"), 
                                 MyRegEx(@"\bat i dræbte\b"), 
                                 MyRegEx(@"\bat i fandt\b"),
                                 MyRegEx(@"\bat i fik\b"),
                                 MyRegEx(@"\bat i finder\b"),
                                 MyRegEx(@"\bat i forstår\b"),
                                 MyRegEx(@"\bat i får\b"),
                                 MyRegEx(@"\b[Aa]t i hver især\b"),                                 
                                 MyRegEx(@"\bAt i ikke\b"),
                                 MyRegEx(@"\bat i ikke\b"),
                                 MyRegEx(@"\bat i kom\b"),
                                 MyRegEx(@"\bat i kommer\b"),
                                 MyRegEx(@"\bat i næsten er\b"),
                                 MyRegEx(@"\bat i næsten fik\b"),
                                 MyRegEx(@"\bat i næsten har\b"),
                                 MyRegEx(@"\bat i næsten skulle\b"),
                                 MyRegEx(@"\bat i næsten var\b"),
                                 MyRegEx(@"\bat i også får\b"),
                                 MyRegEx(@"\bat i også gør\b"),
                                 MyRegEx(@"\bat i også mener\b"),
                                 MyRegEx(@"\bat i også siger\b"),
                                 MyRegEx(@"\bat i også tror\b"),
                                 MyRegEx(@"\bat i rev\b"),
                                 MyRegEx(@"\bat i river\b"),
                                 MyRegEx(@"\bat i samarbejder\b"),                                 
                                 MyRegEx(@"\bat i snakkede\b"),
                                 MyRegEx(@"\bat i scorer\b"), 
                                 MyRegEx(@"\bat i siger\b"),
                                 MyRegEx(@"\bat i skal\b"),
                                 MyRegEx(@"\bat i skulle\b"),
                                 MyRegEx(@"\bat i to ikke\b"),
                                 MyRegEx(@"\bat i to siger\b"),
                                 MyRegEx(@"\bat i to har\b"),
                                 MyRegEx(@"\bat i to er\b"),
                                 MyRegEx(@"\bat i to bager\b"),
                                 MyRegEx(@"\bat i to skal\b"),
                                 MyRegEx(@"\bat i to gør\b"),
                                 MyRegEx(@"\bat i to får\b"),
                                 MyRegEx(@"\bat i udnyttede\b"), 
                                 MyRegEx(@"\bat i udnytter\b"), 
                                 MyRegEx(@"\bat i vil\b"), 
                                 MyRegEx(@"\bat i ville\b"), 
                                 MyRegEx(@"\bBehandler i mig\b"), 
                                 MyRegEx(@"\bbehandler i mig\b"), 
                                 MyRegEx(@"\bbliver i rige\b"), 
                                 MyRegEx(@"\bbliver i ikke\b"),
                                 MyRegEx(@"\bbliver i indkvarteret\b"),                                 
                                 MyRegEx(@"\bbliver i indlogeret\b"),                                 
                                 MyRegEx(@"\bburde i gøre\b"), 
                                 MyRegEx(@"\bburde i ikke\b"), 
                                 MyRegEx(@"\bburde i købe\b"), 
                                 MyRegEx(@"\bburde i løbe\b"), 
                                 MyRegEx(@"\bburde i se\b"), 
                                 MyRegEx(@"\bburde i sige\b"), 
                                 MyRegEx(@"\bburde i tage\b"), 
                                 MyRegEx(@"\bDa i ankom\b"), 
                                 MyRegEx(@"\bda i ankom\b"), 
                                 MyRegEx(@"\bda i forlod\b"), 
                                 MyRegEx(@"\bDa i forlod\b"), 
                                 MyRegEx(@"\bda i fik\b"), 
                                 MyRegEx(@"\bDa i fik\b"), 
                                 MyRegEx(@"\bDa i gik\b"), 
                                 MyRegEx(@"\bda i gik\b"), 
                                 MyRegEx(@"\bda i kom\b"), 
                                 MyRegEx(@"\bDa i kom\b"), 
                                 MyRegEx(@"\bda i så "), 
                                 MyRegEx(@"\bDa i så "), 
                                 MyRegEx(@"\bdet får i\b"), 
                                 MyRegEx(@"\bDet får i\b"), 
                                 MyRegEx(@"\bDet har i\b"), 
                                 MyRegEx(@"\bdet har i\b"), 
                                 MyRegEx(@"\bDet må i "), 
                                 MyRegEx(@"\bdet må i "), 
                                 MyRegEx(@"\bend i aner\b"), 
                                 MyRegEx(@"\bend i tror\b"), 
                                 MyRegEx(@"\bend i ved\b"), 
                                 MyRegEx(@"\ber i alle\b"), 
                                 MyRegEx(@"\bEr i alle\b"), 
                                 MyRegEx(@"\ber i allerede\b"), 
                                 MyRegEx(@"\bEr i allerede\b"), 
                                 MyRegEx(@"\ber i allesammen\b"), 
                                 MyRegEx(@"\bEr i allesammen\b"), 
                                 MyRegEx(@"\ber i der\b"), 
                                 MyRegEx(@"\bEr i der\b"), 
                                 MyRegEx(@"\bEr i fra\b"), 
                                 MyRegEx(@"\bEr i gennem\b"), 
                                 MyRegEx(@"\ber i gennem\b"), 
                                 MyRegEx(@"\ber i glade\b"), 
                                 MyRegEx(@"\bEr i glade\b"), 
                                 MyRegEx(@"\bEr i gået\b"), 
                                 MyRegEx(@"\ber i gået\b"), 
                                 MyRegEx(@"\ber i her\b"), 
                                 MyRegEx(@"\bEr i her\b"), 
                                 MyRegEx(@"\ber i imod\b"), 
                                 MyRegEx(@"\bEr i imod\b"), 
                                 MyRegEx(@"\ber i klar\b"), 
                                 MyRegEx(@"\bEr i klar\b"), 
                                 MyRegEx(@"\bEr i mætte\b"), 
                                 MyRegEx(@"\ber i mætte\b"), 
                                 MyRegEx(@"\bEr i med\b"), 
                                 MyRegEx(@"\ber i med\b"), 
                                 MyRegEx(@"\ber i mod\b"), 
                                 MyRegEx(@"\bEr i mod\b"), 
                                 MyRegEx(@"\ber i okay\b"), 
                                 MyRegEx(@"\bEr i okay\b"), 
                                 MyRegEx(@"\ber i på\b"), 
                                 MyRegEx(@"\bEr i på\b"), 
                                 MyRegEx(@"\bEr i parate\b"), 
                                 MyRegEx(@"\ber i parate\b"), 
                                 MyRegEx(@"\ber i sikker\b"), 
                                 MyRegEx(@"\bEr i sikker\b"), 
                                 MyRegEx(@"\bEr i sikre\b"), 
                                 MyRegEx(@"\ber i sikre\b"), 
                                 MyRegEx(@"\ber i skøre\b"), 
                                 MyRegEx(@"\bEr i skøre\b"), 
                                 MyRegEx(@"\ber i stadig\b"), 
                                 MyRegEx(@"\bEr i stadig\b"), 
                                 MyRegEx(@"\bEr i sultne\b"), 
                                 MyRegEx(@"\ber i sultne\b"), 
                                 MyRegEx(@"\bEr i tilfredse\b"), 
                                 MyRegEx(@"\ber i tilfredse\b"), 
                                 MyRegEx(@"\bEr i to\b"), 
                                 MyRegEx(@"\ber i ved at\b"), 
                                 MyRegEx(@"\ber i virkelig\b"), 
                                 MyRegEx(@"\bEr i virkelig\b"), 
                                 MyRegEx(@"\bEr i vågne\b"), 
                                 MyRegEx(@"\ber i vågne\b"), 
                                 MyRegEx(@"\bfanden vil i?"),
                                 MyRegEx(@"\bfor ser i\b"), 
                                 MyRegEx(@"\bFor ser i\b"), 
                                 MyRegEx(@"\bFordi i ventede\b"), 
                                 MyRegEx(@"\bfordi i ventede\b"), 
                                 MyRegEx(@"\bFordi i deltog\b"), 
                                 MyRegEx(@"\bfordi i deltog\b"), 
                                 MyRegEx(@"\bforhandler i stadig\b"), 
                                 MyRegEx(@"\bForhandler i stadig\b"), 
                                 MyRegEx(@"\bforstår i\b"), 
                                 MyRegEx(@"\bForstår i\b"), 
                                 MyRegEx(@"\bFør i får\b"), 
                                 MyRegEx(@"\bfør i får\b"), 
                                 MyRegEx(@"\bFør i kommer\b"), 
                                 MyRegEx(@"\bfør i kommer\b"), 
                                 MyRegEx(@"\bFør i tager\b"), 
                                 MyRegEx(@"\bfør i tager\b"), 
                                 MyRegEx(@"\bfår i alle\b"), 
                                 MyRegEx(@"\bfår i fratrukket\b"), 
                                 MyRegEx(@"\bfår i ikke\b"), 
                                 MyRegEx(@"\bfår i klø\b"), 
                                 MyRegEx(@"\bfår i point\b"), 
                                 MyRegEx(@"\bgider i at\b"),
                                 MyRegEx(@"\bGider i at\b"),
                                 MyRegEx(@"\bGider i ikke\b"),
                                 MyRegEx(@"\bgider i ikke\b"),
                                 MyRegEx(@"\bgider i lige\b"),
                                 MyRegEx(@"\bGider i lige\b"),
                                 MyRegEx(@"\b[Gg]ik i lige\b"),
                                 MyRegEx(@"\b[Gg]ik i hjem\b"),
                                 MyRegEx(@"\b[Gg]ik i over\b"),
                                 MyRegEx(@"\b[Gg]ik i forbi\b"),
                                 MyRegEx(@"\b[Gg]ik i ind\b"),
                                 MyRegEx(@"\b[Gg]ik i uden\b"),
                                 MyRegEx(@"\bGjorde i det\b"),
                                 MyRegEx(@"\bGjorde i det\b"),
                                 MyRegEx(@"\bgjorde i ikke\b"),
                                 MyRegEx(@"\bGider i godt\b"),
                                 MyRegEx(@"\bgider i godt\b"),
                                 MyRegEx(@"\bGider i ikke\b"),
                                 MyRegEx(@"\bgider i ikke\b"),
                                 MyRegEx(@"\b[Gg]iver i mig\b"),
                                 MyRegEx(@"\bglor i på\b"),
                                 MyRegEx(@"\bGlor i på\b"),
                                 MyRegEx(@"\bGår i ind\b"),
                                 MyRegEx(@"\bgår i ind\b"),
                                 MyRegEx(@"\b[Gg]å i bare\b"),
                                 MyRegEx(@"\bHørte i det\b"),
                                 MyRegEx(@"\bhørte i det\b"),
                                 MyRegEx(@"\bHar i \b"),
                                 MyRegEx(@"\bhar i ødelagt\b"),
                                 MyRegEx(@"\bhar i fået\b"),
                                 MyRegEx(@"\bHar i fået\b"),
                                 MyRegEx(@"\bHar i det\b"),
                                 MyRegEx(@"\bhar i det\b"),
                                 MyRegEx(@"\bhar i gjort\b"),
                                 MyRegEx(@"\bhar i ikke\b"),
                                 MyRegEx(@"\bHar i nogen\b"),
                                 MyRegEx(@"\bhar i nogen\b"),
                                 MyRegEx(@"\bHar i nok\b"),
                                 MyRegEx(@"\bhar i nok\b"),
                                 MyRegEx(@"\bhar i ordnet\b"),     
                                 MyRegEx(@"\bHar i ordnet\b"),     
                                 MyRegEx(@"\bhar i spist\b"),     
                                 MyRegEx(@"\bHar i spist\b"),     
                                 MyRegEx(@"\bhar i tænkt\b"),
                                 MyRegEx(@"\bhar i tabt\b"),
                                 MyRegEx(@"\bhelvede vil i?"),
                                 MyRegEx(@"\bHer har i\b"),
                                 MyRegEx(@"\bher har i\b"),
                                 MyRegEx(@"\bHvad fanden har i\b"), 
                                 MyRegEx(@"\bhvad fanden har i\b"), 
                                 MyRegEx(@"\bHvad fanden tror i\b"), 
                                 MyRegEx(@"\bhvad fanden tror i\b"), 
                                 MyRegEx(@"\bhvad fanden vil i\b"), 
                                 MyRegEx(@"\bHvad fanden vil i\b"), 
                                 MyRegEx(@"\bHvad gør i\b"), 
                                 MyRegEx(@"\bhvad gør i\b"), 
                                 MyRegEx(@"\bhvad har i\b"), 
                                 MyRegEx(@"\bHvad har i\b"), 
                                 MyRegEx(@"\bHvad i ikke\b"), 
                                 MyRegEx(@"\bhvad i ikke\b"), 
                                 MyRegEx(@"\b[Hh]vad laver i\b"), 
                                 MyRegEx(@"\b[Hh]vad lavede i\b"), 
                                 MyRegEx(@"\b[Hh]vad mener i\b"), 
                                 MyRegEx(@"\b[Hh]vad siger i\b"), 
                                 MyRegEx(@"\b[Hh]vad skal i\b"), 
                                 MyRegEx(@"\b[Hh]vad snakker i\b"), 
                                 MyRegEx(@"\b[Hh]vad sløver i\b"), 
                                 MyRegEx(@"\b[Hh]vad synes i\b"), 
                                 MyRegEx(@"\b[Hh]vad vil i\b"),                                  
                                 MyRegEx(@"\b[Hh]vem er i\b"), 
                                 MyRegEx(@"\b[Hh]vem fanden tror i\b"), 
                                 MyRegEx(@"\b[Hh]vem tror i\b"), 
                                 MyRegEx(@"\b[Hh]vilken slags mennesker er i?"), 
                                 MyRegEx(@"\b[Hh]vilken slags folk er i?"), 
                                 MyRegEx(@"\b[Hh]vis i altså\b"), 
                                 MyRegEx(@"\b[Hh]vis i bare\b"), 
                                 MyRegEx(@"\b[Hh]vis i forstår\b"), 
                                 MyRegEx(@"\b[Hh]vis i får\b"), 
                                 MyRegEx(@"\b[Hh]vis i går\b"), 
                                 MyRegEx(@"\b[Hh]vis i ikke\b"), 
                                 MyRegEx(@"\b[Hh]vis i lovede\b"), 
                                 MyRegEx(@"\b[Hh]vis i lover\b"), 
                                 MyRegEx(@"\b[Hh]vis i overholder\b"), 
                                 MyRegEx(@"\b[Hh]vis i overtræder\b"), 
                                 MyRegEx(@"\b[Hh]vis i slipper\b"), 
                                 MyRegEx(@"\b[Hh]vis i taber\b"), 
                                 MyRegEx(@"\b[Hh]vis i vandt\b"), 
                                 MyRegEx(@"\b[Hh]vis i vinder\b"), 
                                 MyRegEx(@"\b[Hh]vor er i\b"), 
                                 MyRegEx(@"\b[Hh]vor får i\b"), 
                                 MyRegEx(@"\b[Hh]vor gamle er i\b"),                                  
                                 MyRegEx(@"\b[Hh]vor i begyndte\b"), 
                                 MyRegEx(@"\b[Hh]vor i startede\b"), 
                                 MyRegEx(@"\b[Hh]vor skal i\b"), 
                                 MyRegEx(@"\b[Hh]vor var i\b"), 
                                 MyRegEx(@"\b[Hh]vordan har i\b"), 
                                 MyRegEx(@"\b[Hh]vordan hørte i\b"), 
                                 MyRegEx(@"\b[Hh]vordan i når\b"), 
                                 MyRegEx(@"\b[Hh]vordan i nåede\b"), 
                                 MyRegEx(@"\b[Hh]vordan kunne i\b"), 
                                 MyRegEx(@"\b[Hh]vorfor afleverer i det\b"), 
                                 MyRegEx(@"\b[Hh]vorfor gør i "), 
                                 MyRegEx(@"\b[Hh]vorfor gjorde i "), 
                                 MyRegEx(@"\b[Hh]vorfor græder i "), 
                                 MyRegEx(@"\b[Hh]vorfor har i "), 
                                 MyRegEx(@"\b[Hh]vorfor kom i "), 
                                 MyRegEx(@"\b[Hh]vorfor kommer i "), 
                                 MyRegEx(@"\b[Hh]vorfor løb i "), 
                                 MyRegEx(@"\b[Hh]vorfor lover i "), 
                                 MyRegEx(@"\b[Hh]vorfor lovede i "), 
                                 MyRegEx(@"\b[Hh]vorfor skal i\b"), 
                                 MyRegEx(@"\b[Hh]vorfor skulle i\b"), 
                                 MyRegEx(@"\b[Hh]vorfor sagde i\b"), 
                                 MyRegEx(@"\b[Hh]vorfor synes i\b"), 
                                 MyRegEx(@"\b[Hh]vornår gør i "), 
                                 MyRegEx(@"\bHvornår kom i\b"), 
                                 MyRegEx(@"\b[Hh]vornår ville i "), 
                                 MyRegEx(@"\b[Hh]vornår giver i "), 
                                 MyRegEx(@"\b[Hh]vornår gav i "), 
                                 MyRegEx(@"\b[Hh]vornår rejser i\b"), 
                                 MyRegEx(@"\b[Hh]vornår rejste i\b"), 
                                 MyRegEx(@"\b[Hh]vornår skal i "), 
                                 MyRegEx(@"\b[Hh]vornår skulle i "), 
                                 MyRegEx(@"\b[Hh]ører i på\b"),                                 
                                 MyRegEx(@"\b[Hh]ørte i på\b"),                                 
                                 MyRegEx(@"\bi altid\b"),
                                 MyRegEx(@"\bi ankomme\b"),
                                 MyRegEx(@"\bi ankommer\b"),
                                 MyRegEx(@"\bi bare kunne\b"),
                                 MyRegEx(@"\bi bare havde\b"),
                                 MyRegEx(@"\bi bare gjorde\b"),
                                 MyRegEx(@"\bi begge er\b"),
                                 MyRegEx(@"\bi begge gør\b"),
                                 MyRegEx(@"\bi begge har\b"),
                                 MyRegEx(@"\bi begge var\b"),
                                 MyRegEx(@"\bi begge vil\b"),
                                 MyRegEx(@"\bi behøver ikke gemme\b"),
                                 MyRegEx(@"\bi behøver ikke prøve\b"),
                                 MyRegEx(@"\bi behøver ikke skjule\b"),
                                 MyRegEx(@"\bi behandlede\b"),
                                 MyRegEx(@"\bi behandler\b"),
                                 MyRegEx(@"\bi beskidte dyr\b"),
                                 MyRegEx(@"\bi blev\b"),
                                 MyRegEx(@"\bi blive\b"),
                                 MyRegEx(@"\bi bliver\b"),
                                 MyRegEx(@"\bi burde\b"),
                                 MyRegEx(@"\bi er\b"),
                                 MyRegEx(@"\bi fyrer af\b"),
                                 MyRegEx(@"\bi gør\b"),
                                 MyRegEx(@"\bi gav\b"),
                                 MyRegEx(@"\bi gerne "),
                                 MyRegEx(@"\bi giver\b"),
                                 MyRegEx(@"\bi gjorde\b"),
                                 MyRegEx(@"\bi hører\b"),
                                 MyRegEx(@"\bi hørte\b"),
                                 MyRegEx(@"\bi har\b"),
                                 MyRegEx(@"\bi havde\b"),
                                 MyRegEx(@"\bi igen bliver\b"),
                                 MyRegEx(@"\bi igen burde\b"),
                                 MyRegEx(@"\bi igen finder\b"),
                                 MyRegEx(@"\bi igen gør\b"),
                                 MyRegEx(@"\bi igen kommer\b"),
                                 MyRegEx(@"\bi igen prøver\b"),
                                 MyRegEx(@"\bi igen siger\b"),
                                 MyRegEx(@"\bi igen skal\b"),
                                 MyRegEx(@"\bi igen vil\b"),
                                 MyRegEx(@"\bi ikke gerne\b"),
                                 MyRegEx(@"\bi ikke kan\b"),
                                 MyRegEx(@"\bi ikke kommer\b"),
                                 MyRegEx(@"\bi ikke vil\b"),
                                 MyRegEx(@"\bi kan\b"),
                                 MyRegEx(@"\bi kender\b"),
                                 MyRegEx(@"\bi kom\b"),
                                 MyRegEx(@"\bi komme\b"),
                                 MyRegEx(@"\bi kommer\b"),
                                 MyRegEx(@"\bi kunne\b"),
                                 MyRegEx(@"\bi morer jer\b"),
                                 MyRegEx(@"\bi må gerne\b"),
                                 MyRegEx(@"\bi må give\b"),
                                 MyRegEx(@"\bi må da\b"),
                                 MyRegEx(@"\bi nåede\b"),
                                 MyRegEx(@"\bi når\b"),                                 
                                 MyRegEx(@"\bi prøve\b"),
                                 MyRegEx(@"\bi prøvede\b"),
                                 MyRegEx(@"\bi prøver\b"),
                                 MyRegEx(@"\bi sagde\b"),
                                 MyRegEx(@"\bi scorede\b"),
                                 MyRegEx(@"\bi ser\b"),
                                 MyRegEx(@"\bi set\b"),
                                 MyRegEx(@"\bi siger\b"),
                                 MyRegEx(@"\bi sikkert alle\b"),
                                 MyRegEx(@"\bi sikkert ikke gør\b"),
                                 MyRegEx(@"\bi sikkert ikke kan\b"),
                                 MyRegEx(@"\bi sikkert ikke vil\b"),
                                 MyRegEx(@"\bi skal\b"),
                                 MyRegEx(@"\bi skulle\b"),
                                 MyRegEx(@"\bi små stakler\b"),                                 
                                 MyRegEx(@"\bi stopper\b"),
                                 MyRegEx(@"\bi synes\b"),
                                 MyRegEx(@"\bi troede\b"),
                                 MyRegEx(@"\bi tror\b"),
                                 MyRegEx(@"\bi var\b"),
                                 MyRegEx(@"\bi vel ikke\b"),
                                 MyRegEx(@"\bi vil\b"),
                                 MyRegEx(@"\bi ville\b"),
                                 MyRegEx(@"\b[Kk]an i lugte\b"), 
                                 MyRegEx(@"\b[Kk]an i overleve\b"), 
                                 MyRegEx(@"\b[Kk]an i spise\b"), 
                                 MyRegEx(@"\b[Kk]an i se\b"), 
                                 MyRegEx(@"\b[Kk]an i smage\b"), 
                                 MyRegEx(@"\b[Kk]an i forstå\b"), 
                                 MyRegEx(@"\b[Kk]ørte i hele\b"), 
                                 MyRegEx(@"\b[Kk]ørte i ikke\b"), 
                                 MyRegEx(@"\b[Kk]an i godt\b"),
                                 MyRegEx(@"\b[Kk]an i gøre\b"),
                                 MyRegEx(@"\b[Kk]an i huske\b"),
                                 MyRegEx(@"\b[Kk]an i ikke\b"),
                                 MyRegEx(@"\b[Kk]an i lide\b"),
                                 MyRegEx(@"\b[Kk]an i leve\b"),
                                 MyRegEx(@"\b[Kk]an i love\b"),
                                 MyRegEx(@"\b[Kk]an i måske\b"),
                                 MyRegEx(@"\b[Kk]an i nok\b"),
                                 MyRegEx(@"\b[Kk]an i se\b"),
                                 MyRegEx(@"\b[Kk]an i sige\b"),
                                 MyRegEx(@"\b[Kk]an i tilgive\b"),
                                 MyRegEx(@"\b[Kk]an i tygge\b"),
                                 MyRegEx(@"\b[Kk]an i to ikke\b"),
                                 MyRegEx(@"\b[Kk]an i tro\b"),
                                 MyRegEx(@"\bKender i "),
                                 MyRegEx(@"\b[Kk]ender i hinanden\b"),
                                 MyRegEx(@"\b[Kk]ender i to hinanden\b"),
                                 MyRegEx(@"\bKendte i \b"),
                                 MyRegEx(@"\b[Kk]endte i hinanden\b"),
                                 MyRegEx(@"\b[Kk]iggede i på\b"),
                                 MyRegEx(@"\b[Kk]igger i på\b"),
                                 MyRegEx(@"\b[Kk]ommer i her\b"),
                                 MyRegEx(@"\b[Kk]ommer i ofte\b"),
                                 MyRegEx(@"\b[Kk]ommer i sammen\b"),
                                 MyRegEx(@"\b[Kk]ommer i tit\b"),
                                 MyRegEx(@"\b[Kk]unne i fortælle\b"),
                                 MyRegEx(@"\b[Kk]unne i give\b"),
                                 MyRegEx(@"\b[Kk]unne i gøre\b"),
                                 MyRegEx(@"\b[Kk]unne i ikke\b"),
                                 MyRegEx(@"\b[Kk]unne i lide\b"), 
                                 MyRegEx(@"\b[Kk]unne i mødes\b"), 
                                 MyRegEx(@"\b[Kk]unne i se\b"), 
                                 MyRegEx(@"\b[Ll]eder i efter\b"), 
                                 MyRegEx(@"\b[Ll]aver i ikke\b"), 
                                 MyRegEx(@"\blaver i her\b"), 
                                 MyRegEx(@"\bLover i\b"), 
                                 MyRegEx(@"\b[Ll]øb i hellere\b"),                                  
                                 MyRegEx(@"\b[Mm]ødte i "),
                                 MyRegEx(@"\b[Mm]angler i en\b"), 
                                 MyRegEx(@"\b[Mm]en i gutter\b"),
                                 MyRegEx(@"\b[Mm]en i drenge\b"),
                                 MyRegEx(@"\b[Mm]en i fyre\b"),
                                 MyRegEx(@"\b[Mm]ener i at\b"),
                                 MyRegEx(@"\b[Mm]ener i det\b"),
                                 MyRegEx(@"\b[Mm]ener i virkelig\b"),
                                 MyRegEx(@"\b[Mm]ens i sov\b"),
                                 MyRegEx(@"\b[Mm]ens i stadig\b"),
                                 MyRegEx(@"\b[Mm]ens i lå\b"),
                                 MyRegEx(@"\b[Mm]ister i point\b"), 
                                 MyRegEx(@"\b[Mm]orer i jer\b"),
                                 MyRegEx(@"\b[Mm]å i alle"),
                                 MyRegEx(@"\b[Mm]å i gerne"),
                                 MyRegEx(@"\b[Mm]å i godt\b"),
                                 MyRegEx(@"\b[Mm]å i vide\b"),
                                 MyRegEx(@"\b[Mm]å i ikke"),
                                 MyRegEx(@"\b[Nn]u løber i\b"),
                                 MyRegEx(@"\b[Nn]u siger i\b"),
                                 MyRegEx(@"\b[Nn]u skal i\b"),
                                 MyRegEx(@"\b[Nn]år i\b"),
                                 MyRegEx(@"\b[Oo]m i ikke\b"),
                                 MyRegEx(@"\b[Oo]pgiver i\b"),
                                 MyRegEx(@"\b[Oo]vergiver i jer\b"),
                                 MyRegEx(@"\bpersoner i lukker\b"),
                                 MyRegEx(@"\b[Pp]as på i ikke\b"),
                                 MyRegEx(@"\b[Pp]as på i ikke\b"),
                                 MyRegEx(@"\b[Pp]å i ikke\b"),
                                 MyRegEx(@"\b[Pp]å at i ikke\b"),
                                 MyRegEx(@"\b[Ss]agde i ikke\b"),
                                 MyRegEx(@"\b[Ss]amlede i ham\b"),
                                 MyRegEx(@"\bSer i\b"), 
                                 MyRegEx(@"\bSiger i\b"), 
                                 MyRegEx(@"\b[Ss]ikker på i ikke\b"),
                                 MyRegEx(@"\b[Ss]ikre på i ikke\b"),
                                 MyRegEx(@"\b[Ss]kal i alle\b"), 
                                 MyRegEx(@"\b[Ss]kal i allesammen\b"), 
                                 MyRegEx(@"\b[Ss]kal i bare\b"), 
                                 MyRegEx(@"\b[Ss]kal i dele\b"), 
                                 MyRegEx(@"\b[Ss]kal i fordele\b"), 
                                 MyRegEx(@"\b[Ss]kal i fordeles\b"), 
                                 MyRegEx(@"\b[Ss]kal i gøre\b"), 
                                 MyRegEx(@"\b[Ss]kal i have\b"), 
                                 MyRegEx(@"\b[Ss]kal i ikke\b"), 
                                 MyRegEx(@"\b[Ss]kal i klare\b"), 
                                 MyRegEx(@"\b[Ss]kal i klatre\b"), 
                                 MyRegEx(@"\b[Ss]kal i larme\b"), 
                                 MyRegEx(@"\b[Ss]kal i lave\b"), 
                                 MyRegEx(@"\b[Ss]kal i løfte\b"), 
                                 MyRegEx(@"\b[Ss]kal i med\b"), 
                                 MyRegEx(@"\b[Ss]kal i på\b"), 
                                 MyRegEx(@"\b[Ss]kal i til\b"), 
                                 MyRegEx(@"\b[Ss]kal i ud\b"), 
                                 MyRegEx(@"\b[Ss]lap i ud\b"), 
                                 MyRegEx(@"\b[Ss]lap i væk\b"), 
                                 MyRegEx(@"\b[Ss]nart er i\b"), 
                                 MyRegEx(@"\b[Ss]om i måske\b"),
                                 MyRegEx(@"\b[Ss]om i nok\b"),
                                 MyRegEx(@"\b[Ss]om i ved\b"),
                                 MyRegEx(@"\b[Ss]pis i bare\b"), 
                                 MyRegEx(@"\b[Ss]pis i dem\b"), 
                                 MyRegEx(@"\b[Ss]ynes i at\b"),
                                 MyRegEx(@"\b[Ss]ynes i det\b"),
                                 MyRegEx(@"\b[Ss]ynes i,"),                                
                                 MyRegEx(@"\b[Ss]ætter i en\b"), 
                                 MyRegEx(@"\bSå i at\b"), 
                                 MyRegEx(@"\bSå i det\b"), 
                                 MyRegEx(@"\bSå i noget\b"), 
                                 MyRegEx(@"\b[Ss]å tager i\b"), 
                                 MyRegEx(@"\bTænder i på\b"),
                                 MyRegEx(@"\btænder i på\b"),
                                 MyRegEx(@"\btog i bilen\b"),
                                 MyRegEx(@"\bTog i bilen\b"),
                                 MyRegEx(@"\btog i liften\b"),
                                 MyRegEx(@"\bTog i liften\b"),
                                 MyRegEx(@"\btog i toget\b"),
                                 MyRegEx(@"\bTog i toget\b"),
                                 MyRegEx(@"\btræder i frem\b"),
                                 MyRegEx(@"\bTræder i frem\b"),
                                 MyRegEx(@"\bTror i at\b"),
                                 MyRegEx(@"\btror i at\b"),
                                 MyRegEx(@"\btror i det\b"),
                                 MyRegEx(@"\bTror i det\b"),
                                 MyRegEx(@"\bTror i jeg\b"),
                                 MyRegEx(@"\btror i jeg\b"),
                                 MyRegEx(@"\bTror i på\b"),
                                 MyRegEx(@"\b[Tr]ror i på\b"),
                                 MyRegEx(@"\b[Tr]ror i, "),
                                 MyRegEx(@"\b[Vv]ar i blevet\b"),
                                 MyRegEx(@"\b[Vv]ed i alle\b"),
                                 MyRegEx(@"\b[Vv]ed i allesammen\b"),
                                 MyRegEx(@"\b[Vv]ed i er\b"),
                                 MyRegEx(@"\b[Vv]ed i ikke\b"),
                                 MyRegEx(@"\b[Vv]ed i hvad\b"), 
                                 MyRegEx(@"\b[Vv]ed i hvem\b"), 
                                 MyRegEx(@"\b[Vv]ed i hvor\b"), 
                                 MyRegEx(@"\b[Vv]ed i hvorfor\b"), 
                                 MyRegEx(@"\b[Vv]ed i hvordan\b"), 
                                 MyRegEx(@"\b[Vv]ed i var\b"),
                                 MyRegEx(@"\b[Vv]ed i ville\b"),
                                 MyRegEx(@"\b[Vv]ed i har\b"),
                                 MyRegEx(@"\b[Vv]ed i havde\b"),
                                 MyRegEx(@"\b[Vv]ed i hvem\b"),
                                 MyRegEx(@"\b[Vv]ed i hvad\b"),
                                 MyRegEx(@"\b[Vv]ed i hvor\b"),
                                 MyRegEx(@"\b[Vv]ed i mente\b"),
                                 MyRegEx(@"\b[Vv]ed i tror\b"),
                                 MyRegEx(@"\b[Vv]enter i på\b"),
                                 MyRegEx(@"\b[Vv]il i besegle\b"),
                                 MyRegEx(@"\b[Vv]il i dræbe\b"),
                                 MyRegEx(@"\b[Vv]il i fortryde\b"),
                                 MyRegEx(@"\b[Vv]il i gerne\b"),
                                 MyRegEx(@"\b[Vv]il i godt\b"),
                                 MyRegEx(@"\b[Vv]il i have\b"),
                                 MyRegEx(@"\b[Vv]il i høre\b"),
                                 MyRegEx(@"\b[Vv]il i ikke\b"),
                                 MyRegEx(@"\b[Vv]il i købe\b"),
                                 MyRegEx(@"\b[Vv]il i kaste\b"),
                                 MyRegEx(@"\b[Vv]il i møde\b"),
                                 MyRegEx(@"\b[Vv]il i måske\b"),
                                 MyRegEx(@"\bvil i savne\b"),
                                 MyRegEx(@"\bVil i savne\b"),
                                 MyRegEx(@"\bvil i se\b"),
                                 MyRegEx(@"\bVil i se\b"),
                                 MyRegEx(@"\bvil i sikkert\b"),
                                 MyRegEx(@"\bvil i smage\b"),
                                 MyRegEx(@"\bVil i smage\b"),
                                 MyRegEx(@"\b[Vv]il i virkelig\b"),
                                 MyRegEx(@"\b[Vv]il i virkeligt\b"),
                                 MyRegEx(@"\bVil i være\b"),
                                 MyRegEx(@"\bvil i være\b"),
                                 MyRegEx(@"\bVille i blive\b"),
                                 MyRegEx(@"\bville i blive\b"),
                                 MyRegEx(@"\bville i dræbe\b"),
                                 MyRegEx(@"\bville i få\b"),
                                 MyRegEx(@"\bville i få\b"),
                                 MyRegEx(@"\bville i gøre\b"),
                                 MyRegEx(@"\bville i høre\b"),
                                 MyRegEx(@"\bville i ikke\b"),
                                 MyRegEx(@"\bville i kaste\b"),
                                 MyRegEx(@"\bville i komme\b"),
                                 MyRegEx(@"\bville i mene\b"),
                                 MyRegEx(@"\bville i nå\b"),
                                 MyRegEx(@"\bville i savne\b"),
                                 MyRegEx(@"\bVille i se\b"),
                                 MyRegEx(@"\bville i se\b"),
                                 MyRegEx(@"\bville i sikkert\b"),
                                 MyRegEx(@"\bville i synes\b"),
                                 MyRegEx(@"\bville i tage\b"),
                                 MyRegEx(@"\bville i tro\b"),
                                 MyRegEx(@"\bville i være\b"),
                                 MyRegEx(@"\bville i være\b"),                            
                                 MyRegEx(@"\bvover i\b"),                            
                          
                             };

            Regex regExIDag = new Regex(@"\bidag\b", RegexOptions.Compiled);
            Regex regExIGaar = new Regex(@"\bigår\b", RegexOptions.Compiled);
            Regex regExIMorgen = new Regex(@"\bimorgen\b", RegexOptions.Compiled);
            Regex regExIAlt = new Regex(@"\bialt\b", RegexOptions.Compiled);
            Regex regExIGang = new Regex(@"\bigang\b", RegexOptions.Compiled);
            Regex regExIStand = new Regex(@"\bistand\b", RegexOptions.Compiled);
            Regex regExIOevrigt = new Regex(@"\biøvrigt\b", RegexOptions.Compiled);

            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                string text = _subtitle.Paragraphs[i].Text;
                string oldText = text;

                if (littleIRegex.IsMatch(text))
                {
                    foreach (Regex regex in iList)
                    {
                        Match match = regex.Match(text);
                        while (match.Success)
                        {
                            Match iMatch = littleIRegex.Match(match.Value);
                            if (iMatch.Success)
                            {
                                string temp = match.Value.Remove(iMatch.Index, 1).Insert(iMatch.Index, "I");

                                int index = match.Index;
                                if (index + match.Value.Length >= text.Length)
                                    text = text.Substring(0, index) + temp;
                                else
                                    text = text.Substring(0, index) + temp + text.Substring(index + match.Value.Length);
                            }
                            match = match.NextMatch();
                        }
                    }
                }

                if (regExIDag.IsMatch(text))
                    text = regExIDag.Replace(text, "i dag");

                if (regExIGaar.IsMatch(text))
                    text = regExIGaar.Replace(text, "i går");

                if (regExIMorgen.IsMatch(text))
                    text = regExIMorgen.Replace(text, "i morgen");

                if (regExIAlt.IsMatch(text))
                    text = regExIAlt.Replace(text, "i alt");

                if (regExIGang.IsMatch(text))
                    text = regExIGang.Replace(text, "i gang");

                if (regExIStand.IsMatch(text))
                    text = regExIStand.Replace(text, "i stand");

                if (regExIOevrigt.IsMatch(text))
                    text = regExIOevrigt.Replace(text, "i øvrigt");

                if (text != oldText)
                {
                    _subtitle.Paragraphs[i].Text = text;
                    fixCount++;
                    _totalFixes++;
                    AddFixToListView(_subtitle.Paragraphs[i], i + 1, fixAction, oldText, text);
                }
            }
            if (fixCount > 0)
                LogStatus(_language.FixDanishLetterI, string.Format(_language.XIsChangedToUppercase, fixCount));
        }

        /// <summary>
        /// Will try to fix issues with Spanish special letters ¿? and ¡!.
        /// Sentences ending with "?" must start with "¿".
        /// Sentences ending with "!" must start with "¡".
        /// </summary>  
        private void FixSpanishInvertedQuestionAndExclamationMarks()
        { 
            string fixAction = _language.FixSpanishInvertedQuestionAndExclamationMarks; 
            int fixCount = 0;
            for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = _subtitle.Paragraphs[i];
                Paragraph last = _subtitle.GetParagraphOrDefault(i - 1);

                bool wasLastLineClosed = last == null || last.Text.EndsWith("?") || last.Text.EndsWith("!") || last.Text.EndsWith(".") || 
                                         last.Text.EndsWith(":") || last.Text.EndsWith(")") || last.Text.EndsWith("]");
                string trimmedStart = p.Text.TrimStart(("- ").ToCharArray());
                if (last != null && last.Text.EndsWith("...") && trimmedStart.Length > 0 && trimmedStart[0].ToString() == trimmedStart[0].ToString().ToLower())
                    wasLastLineClosed = false;
                if (!wasLastLineClosed &&  last != null && last.Text == last.Text.ToUpper())
                    wasLastLineClosed = true;

                string oldText = p.Text;

                FixSpanishInvertedLetter("?", "¿", p, last, ref wasLastLineClosed, i, fixAction, ref fixCount);
                FixSpanishInvertedLetter("!", "¡", p, last, ref wasLastLineClosed, i, fixAction, ref fixCount);
                //if (p.Text.Contains("?"))
                //{
                //    bool skip = false;
                //    if (last != null && p.Text.Contains("?") && !p.Text.Contains("¿") && last.Text.Contains("¿") && !last.Text.Contains("?"))
                //        skip = true;

                //    if (!skip)
                //    {
                //        int startIndex = 0;
                //        int questionMarkIndex = p.Text.IndexOf("?");
                //        if (!wasLastLineClosed && ((p.Text.IndexOf("!") > 0 && p.Text.IndexOf("!") < questionMarkIndex) ||
                //                                   (p.Text.IndexOf("?") > 0 && p.Text.IndexOf("?") < questionMarkIndex) ||
                //                                   (p.Text.IndexOf(".") > 0 && p.Text.IndexOf(".") < questionMarkIndex)))
                //            wasLastLineClosed = true;
                //        while (questionMarkIndex > 0 && startIndex < p.Text.Length)
                //        {
                //            int inverseQuestionMarkIndex = p.Text.IndexOf("¿", startIndex);
                //            if (wasLastLineClosed && (inverseQuestionMarkIndex == -1 || inverseQuestionMarkIndex > questionMarkIndex))
                //            {
                //                if (AllowFix(i + 1, fixAction))
                //                {
                //                    int j = questionMarkIndex - 1;

                //                    while (j > startIndex && (p.Text[j] == '.' || p.Text[j] == '!' || p.Text[j] == '?'))
                //                        j--;

                //                    while (j > startIndex && p.Text[j] != '.' && p.Text[j] != '!' && p.Text[j] != '?' &&
                //                           !(j > 3 && p.Text.Substring(j - 3, 3) == Environment.NewLine + "-") &&
                //                           !(j > 4 && p.Text.Substring(j - 4, 4) == Environment.NewLine + " -") &&
                //                           !(j > 6 && p.Text.Substring(j - 6, 6) == Environment.NewLine + "<i>-"))
                //                        j--;

                //                    if (".!?".Contains(p.Text[j].ToString()))
                //                    {
                //                        j++;
                //                    }
                //                    if (j + 3 < p.Text.Length && p.Text.Substring(j + 1, 2) == Environment.NewLine)
                //                    {
                //                        j += 3;
                //                    }
                //                    else if (j + 2 < p.Text.Length && p.Text.Substring(j, 2) == Environment.NewLine)
                //                    {
                //                        j += 2;
                //                    }
                //                    if (j >= startIndex)
                //                    {
                //                        string part = p.Text.Substring(j, questionMarkIndex - j + 1);

                //                        string speaker = string.Empty;
                //                        int speakerEnd = part.IndexOf(")");
                //                        if (part.StartsWith("(") && speakerEnd > 0 && speakerEnd < part.IndexOf("?"))
                //                        {
                //                            while (Environment.NewLine.Contains(part[speakerEnd + 1].ToString()))
                //                                speakerEnd++;
                //                            speaker = part.Substring(0, speakerEnd + 1);
                //                            part = part.Substring(speakerEnd + 1);
                //                        }
                //                        speakerEnd = part.IndexOf("]");
                //                        if (part.StartsWith("[") && speakerEnd > 0 && speakerEnd < part.IndexOf("?"))
                //                        {
                //                            while (Environment.NewLine.Contains(part[speakerEnd + 1].ToString()))
                //                                speakerEnd++;
                //                            speaker = part.Substring(0, speakerEnd + 1);
                //                            part = part.Substring(speakerEnd + 1);
                //                        }

                //                        StripableText st = new StripableText(part);
                //                        p.Text = p.Text.Remove(j, questionMarkIndex - j + 1).Insert(j, speaker + st.Pre + "¿" + st.StrippedText + st.Post);
                //                    }
                //                }
                //            }
                //            else if (last != null && !wasLastLineClosed && inverseQuestionMarkIndex == p.Text.IndexOf("?") && !last.Text.Contains("¿"))
                //            {
                //                string lastOldtext = last.Text;
                //                int idx = last.Text.Length - 2;
                //                while (idx > 0 && (last.Text.Substring(idx, 2) != ". ") && (last.Text.Substring(idx, 2) != "! ") && (last.Text.Substring(idx, 2) != "? "))
                //                    idx--;

                //                last.Text = last.Text.Insert(idx, "¿");
                //                fixCount++;
                //                _totalFixes++;
                //                AddFixToListView(p, i, fixAction, lastOldtext, last.Text);
                //            }

                //            startIndex = questionMarkIndex + 2;
                //            if (startIndex < p.Text.Length)
                //                questionMarkIndex = p.Text.IndexOf("?", startIndex);
                //            else
                //                questionMarkIndex = -1;
                //            wasLastLineClosed = true;
                //        }
                //    }
                //    if (p.Text.EndsWith("?...") && p.Text.Length > 4)
                //    {
                //        p.Text = p.Text.Remove(p.Text.Length - 4, 4) + "...?";
                //    }
                //}
                //if (p.Text.Contains("!"))
                //{
                //    bool skip = false;
                //    if (last != null && p.Text.Contains("!") && !p.Text.Contains("¡") && last.Text.Contains("¡") && !last.Text.Contains("!"))
                //        skip = true;

                //    if (!skip)
                //    {
                //        int startIndex = 0;
                //        int exclamationMarkIndex = p.Text.IndexOf("!");
                //        while (exclamationMarkIndex > 0 && startIndex < p.Text.Length)
                //        {
                //            int inversExclamationMarkIndex = p.Text.IndexOf("¡", startIndex);
                //            if (wasLastLineClosed && (inversExclamationMarkIndex == -1 || inversExclamationMarkIndex > exclamationMarkIndex))
                //            {
                //                if (AllowFix(i + 1, fixAction))
                //                {

                //                    int j = exclamationMarkIndex - 1;

                //                    while (j > startIndex && (p.Text[j] == '.' || p.Text[j] == '!' || p.Text[j] == '?'))
                //                        j--;

                //                    while (j > startIndex && p.Text[j] != '.' && p.Text[j] != '!' && p.Text[j] != '?' &&
                //                           !(j > 3 && p.Text.Substring(j - 3, 3) == Environment.NewLine + "-") &&
                //                           !(j > 4 && p.Text.Substring(j - 4, 4) == Environment.NewLine + " -") &&
                //                           !(j > 6 && p.Text.Substring(j - 6, 6) == Environment.NewLine + "<i>-"))
                //                    {
                //                        j--;
                //                    }
                //                    if (".!?".Contains(p.Text[j].ToString()))
                //                    {
                //                        j++;
                //                    }
                //                    if (j + 3 < p.Text.Length && p.Text.Substring(j + 1, 2) == Environment.NewLine)
                //                    {
                //                        j += 3;
                //                    }
                //                    else if (j + 2 < p.Text.Length && p.Text.Substring(j, 2) == Environment.NewLine)
                //                    {
                //                        j += 2;
                //                    }
                //                    if (j >= startIndex)
                //                    {
                //                        string part = p.Text.Substring(j, exclamationMarkIndex - j + 1);

                //                        string speaker = string.Empty;
                //                        int speakerEnd = part.IndexOf(")");
                //                        if (part.StartsWith("(") && speakerEnd > 0 && speakerEnd < part.IndexOf("!"))
                //                        {
                //                            while (Environment.NewLine.Contains(part[speakerEnd + 1].ToString()))
                //                                speakerEnd++;
                //                            speaker = part.Substring(0, speakerEnd + 1);
                //                            part = part.Substring(speakerEnd + 1);
                //                        }
                //                        speakerEnd = part.IndexOf("]");
                //                        if (part.StartsWith("[") && speakerEnd > 0 && speakerEnd < part.IndexOf("!"))
                //                        {
                //                            while (Environment.NewLine.Contains(part[speakerEnd + 1].ToString()))
                //                                speakerEnd++;
                //                            speaker = part.Substring(0, speakerEnd + 1);
                //                            part = part.Substring(speakerEnd + 1);
                //                        }

                //                        StripableText st = new StripableText(part);
                //                        p.Text = p.Text.Remove(j, exclamationMarkIndex - j + 1).Insert(j, speaker + st.Pre + "¡" + st.StrippedText + st.Post);
                //                    }
                //                }
                //            }
                //            else if (last != null && !wasLastLineClosed && exclamationMarkIndex == p.Text.IndexOf("!") && !last.Text.Contains("¡"))
                //            {
                //                string lastOldtext = last.Text;
                //                int idx = last.Text.Length - 2;
                //                while (idx > 0 && (last.Text.Substring(idx, 2) != ". ") && (last.Text.Substring(idx, 2) != "! ") && (last.Text.Substring(idx, 2) != "? "))
                //                    idx--;

                //                last.Text = last.Text.Insert(idx, "¡");
                //                fixCount++;
                //                _totalFixes++;
                //                AddFixToListView(p, i, fixAction, lastOldtext, last.Text);

                //            }
                //            startIndex = exclamationMarkIndex + 2;
                //            if (startIndex < p.Text.Length)
                //                exclamationMarkIndex = p.Text.IndexOf("!", startIndex);
                //            else
                //                exclamationMarkIndex = -1;
                //            wasLastLineClosed = true;
                //        } 
                //    }
                //    if (p.Text.EndsWith("!...") && p.Text.Length > 4)
                //    {
                //        p.Text = p.Text.Remove(p.Text.Length - 4, 4) + "...!";
                //    }
                //}

                if (p.Text != oldText)
                {
                    fixCount++;
                    _totalFixes++;
                    AddFixToListView(p, i + 1, fixAction, oldText, p.Text);
                }

            }
            if (fixCount > 0)
                LogStatus(_language.FixSpanishInvertedQuestionAndExclamationMarks, fixCount.ToString());
        }

        private void FixSpanishInvertedLetter(string mark, string inverseMark, Paragraph p, Paragraph last, ref bool wasLastLineClosed, int i, string fixAction, ref int fixCount)
        {
            if (p.Text.Contains(mark))
            {
                bool skip = false;
                if (last != null && p.Text.Contains(mark) && !p.Text.Contains(inverseMark) && last.Text.Contains(inverseMark) && !last.Text.Contains(mark))
                    skip = true;

                if (!skip)
                {
                    int startIndex = 0;
                    int markIndex = p.Text.IndexOf(mark);
                    if (!wasLastLineClosed && ((p.Text.IndexOf("!") > 0 && p.Text.IndexOf("!") < markIndex) ||
                                               (p.Text.IndexOf("?") > 0 && p.Text.IndexOf("?") < markIndex) ||
                                               (p.Text.IndexOf(".") > 0 && p.Text.IndexOf(".") < markIndex)))
                        wasLastLineClosed = true;
                    while (markIndex > 0 && startIndex < p.Text.Length)
                    {
                        int inverseMarkIndex = p.Text.IndexOf(inverseMark, startIndex);
                        if (wasLastLineClosed && (inverseMarkIndex == -1 || inverseMarkIndex > markIndex))
                        {
                            if (AllowFix(i + 1, fixAction))
                            {
                                int j = markIndex - 1;

                                while (j > startIndex && (p.Text[j] == '.' || p.Text[j] == '!' || p.Text[j] == '?'))
                                    j--;

                                while (j > startIndex &&
                                       (p.Text[j] != '.' || IsSpanishAbbreviation(p.Text, j)) && 
                                       p.Text[j] != '!' && 
                                       p.Text[j] != '?' &&
                                       !(j > 3 && p.Text.Substring(j - 3, 3) == Environment.NewLine + "-") &&
                                       !(j > 4 && p.Text.Substring(j - 4, 4) == Environment.NewLine + " -") &&
                                       !(j > 6 && p.Text.Substring(j - 6, 6) == Environment.NewLine + "<i>-"))
                                    j--;

                                if (".!?".Contains(p.Text[j].ToString()))
                                {
                                    j++;
                                }
                                if (j + 3 < p.Text.Length && p.Text.Substring(j + 1, 2) == Environment.NewLine)
                                {
                                    j += 3;
                                }
                                else if (j + 2 < p.Text.Length && p.Text.Substring(j, 2) == Environment.NewLine)
                                {
                                    j += 2;
                                }
                                if (j >= startIndex)
                                {
                                    string part = p.Text.Substring(j, markIndex - j + 1);

                                    string speaker = string.Empty;
                                    int speakerEnd = part.IndexOf(")");
                                    if (part.StartsWith("(") && speakerEnd > 0 && speakerEnd < part.IndexOf(mark))
                                    {
                                        while (Environment.NewLine.Contains(part[speakerEnd + 1].ToString()))
                                            speakerEnd++;
                                        speaker = part.Substring(0, speakerEnd + 1);
                                        part = part.Substring(speakerEnd + 1);
                                    }
                                    speakerEnd = part.IndexOf("]");
                                    if (part.StartsWith("[") && speakerEnd > 0 && speakerEnd < part.IndexOf(mark))
                                    {
                                        while (Environment.NewLine.Contains(part[speakerEnd + 1].ToString()))
                                            speakerEnd++;
                                        speaker = part.Substring(0, speakerEnd + 1);
                                        part = part.Substring(speakerEnd + 1);
                                    }

                                    StripableText st = new StripableText(part);
                                    p.Text = p.Text.Remove(j, markIndex - j + 1).Insert(j, speaker + st.Pre + inverseMark + st.StrippedText + st.Post);
                                }
                            }
                        }
                        else if (last != null && !wasLastLineClosed && inverseMarkIndex == p.Text.IndexOf(mark) && !last.Text.Contains(inverseMark))
                        {
                            string lastOldtext = last.Text;
                            int idx = last.Text.Length - 2;
                            while (idx > 0 && (last.Text.Substring(idx, 2) != ". ") && (last.Text.Substring(idx, 2) != "! ") && (last.Text.Substring(idx, 2) != "? "))
                                idx--;

                            last.Text = last.Text.Insert(idx, inverseMark);
                            fixCount++;
                            _totalFixes++;
                            AddFixToListView(p, i, fixAction, lastOldtext, last.Text);
                        }

                        startIndex = markIndex + 2;
                        if (startIndex < p.Text.Length)
                            markIndex = p.Text.IndexOf(mark, startIndex);
                        else
                            markIndex = -1;
                        wasLastLineClosed = true;
                    }
                }
                if (p.Text.EndsWith(mark + "...") && p.Text.Length > 4)
                {
                    p.Text = p.Text.Remove(p.Text.Length - 4, 4) + "..." + mark;
                }
            }
        }

        private bool IsSpanishAbbreviation(string text, int index)
        {
            if (text[index] != '.')
                return false;
                
            if (index +3 < text.Length && text[index+2] == '.') //  X
                return true;                                    // O.R.

            if (index -3 > 0 && text[index-1] != '.' && text[index-2] == '.') //    X
                return true;                          // O.R.

            string word = string.Empty;
            int i = index-1;
            while (i >= 0 && Utilities.GetLetters(true, true, false).Contains(text[i].ToString()))
            {
                word = text[i].ToString() + word;
                i--;
            }

            //Common Spanish abbreviations
            //Dr. (same as english)
            //Sr. (same as Mr.)
            //Sra. (same as Mrs.)
            //Ud.
            //Uds.
            if (word.ToLower() == "dr" || word.ToLower() == "sr" || word.ToLower() == "sra" || word.ToLower() == "ud" || word.ToLower() == "uds")
                return true;

            List<string> abbreviations = GetAbbreviations();
            return abbreviations.Contains(word + ".");
        }

        private void ButtonFixClick(object sender, EventArgs e)
        {
            if (buttonBack.Enabled)
            {
                Cursor = Cursors.WaitCursor;
                SaveConfiguration();
                Cursor = Cursors.Default;
                DialogResult = DialogResult.OK;
            }
            else
            {
                if (listView1.Items[IndexAloneLowercaseIToUppercaseIEnglish].Checked &&
                    _autoDetectGoogleLanguage != "en")
                {
                    if (MessageBox.Show(_language.FixLowercaseIToUppercaseICheckedButCurrentLanguageIsNotEnglish + Environment.NewLine +
                                                      Environment.NewLine +
                                                      _language.ContinueAnyway, _language.Continue, MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        listView1.Items[IndexAloneLowercaseIToUppercaseIEnglish].Checked = false;
                        ShowStatus(_language.UncheckedFixLowercaseIToUppercaseI);
                        return;
                    }
                }
                Cursor = Cursors.WaitCursor;
                Next();
                ShowAvailableFixesStatus();
            }
            Cursor = Cursors.Default;
        }

        private void Next()
        {

            RunSelectedActions();

            buttonBack.Enabled = true;
            buttonNextFinish.Text = _languageGeneral.OK;
            buttonNextFinish.Enabled = _hasFixesBeenMade;
            groupBoxStep1.Visible = false;
            groupBox2.Visible = true;
            listViewFixes.Sort();
            subtitleListView1.Fill(_originalSubtitle);
            if (listViewFixes.Items.Count > 0)
                listViewFixes.Items[0].Selected = true;
        }

        private void RunSelectedActions()
        {
            subtitleListView1.BeginUpdate();
            _newLog = new StringBuilder();

            _subtitle = new Subtitle(_originalSubtitle);
            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Checked && item.Index != IndexRemoveEmptyLines)
                {
                    FixItem fixItem = (FixItem)item.Tag;
                    fixItem.Action.Invoke(null, null);
                }
            }
            if (listView1.Items[IndexRemoveEmptyLines].Checked)
            {
                FixItem fixItem = (FixItem)listView1.Items[IndexRemoveEmptyLines].Tag;
                fixItem.Action.Invoke(null, null);
            }

            // build log
            textBoxFixedIssues.Text = string.Empty;
            if (_newLog.Length >= 0)
                textBoxFixedIssues.AppendText(_newLog.ToString() + Environment.NewLine);
            textBoxFixedIssues.AppendText(_appliedLog.ToString());
            subtitleListView1.EndUpdate();
        }

        private void FormFix_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                DialogResult = DialogResult.Cancel;
            else if (e.KeyCode == Keys.F1)
                Utilities.ShowHelp("#fixcommonerrors");
            else if (e.KeyCode == Keys.Enter && buttonNextFinish.Text == _language.Next)
                ButtonFixClick(null, null);
        }

        private void SaveConfiguration()
        {
            FixCommonErrorsSettings ce = Configuration.Settings.CommonErrors;

            ce.EmptyLinesTicked = listView1.Items[IndexRemoveEmptyLines].Checked;
            ce.OverlappingDisplayTimeTicked = listView1.Items[IndexOverlappingDisplayTime].Checked;
            ce.TooShortDisplayTimeTicked = listView1.Items[IndexTooShortDisplayTime].Checked;
            ce.TooLongDisplayTimeTicked = listView1.Items[IndexTooLongDisplayTime].Checked;
            ce.InvalidItalicTagsTicked = listView1.Items[IndexInvalidItalicTags].Checked;
            ce.UnneededSpacesTicked = listView1.Items[IndexUnneededSpaces].Checked;
            ce.UnneededPeriodsTicked = listView1.Items[IndexUnneededPeriods].Checked;
            ce.MissingSpacesTicked = listView1.Items[IndexMissingSpaces].Checked;
            ce.BreakLongLinesTicked = listView1.Items[IndexBreakLongLines].Checked;
            ce.MergeShortLinesTicked = listView1.Items[IndexMergeShortLines].Checked;
            ce.UppercaseIInsideLowercaseWordTicked = listView1.Items[IndexUppercaseIInsideLowercaseWord].Checked;
            ce.DoubleApostropheToQuoteTicked = listView1.Items[IndexDoubleApostropheToQuote].Checked;
            ce.FixMusicNotationTicked = listView1.Items[IndexFixMusicNotation].Checked; 
            ce.AddPeriodAfterParagraphTicked = listView1.Items[IndexAddPeriodAfterParagraph].Checked;
            ce.StartWithUppercaseLetterAfterParagraphTicked = listView1.Items[IndexStartWithUppercaseLetterAfterParagraph].Checked;
            ce.StartWithUppercaseLetterAfterPeriodInsideParagraphTicked = listView1.Items[IndexStartWithUppercaseLetterAfterPeriodInsideParagraph].Checked;
            ce.AddMissingQuotesTicked = listView1.Items[IndexAddMissingQuotes].Checked;
            ce.FixHyphensTicked = listView1.Items[IndexFixHyphens].Checked;
            ce.Fix3PlusLinesTicked = listView1.Items[IndexFix3PlusLines].Checked;
            ce.FixDoubleDashTicked = listView1.Items[IndexFixDoubleDash].Checked;
            ce.FixDoubleGreaterThanTicked = listView1.Items[IndexFixDoubleGreaterThan].Checked;
            ce.FixEllipsesStartTicked = listView1.Items[IndexFixEllipsesStart].Checked;
            ce.FixMissingOpenBracketTicked = listView1.Items[IndexFixMissingOpenBracket].Checked;
            ce.AloneLowercaseIToUppercaseIEnglishTicked = listView1.Items[IndexAloneLowercaseIToUppercaseIEnglish].Checked;
            ce.FixOcrErrorsViaReplaceListTicked = listView1.Items[IndexFixOcrErrorsViaReplaceList].Checked;
            if (_danishLetterIIndex > -1)
                ce.DanishLetterITicked = listView1.Items[_danishLetterIIndex].Checked;

            if (_spanishInvertedQuestionAndExclamationMarksIndex > -1)
                ce.SpanishInvertedQuestionAndExclamationMarksTicked = listView1.Items[_spanishInvertedQuestionAndExclamationMarksIndex].Checked;
                
            

            Configuration.Settings.Save();
        }

        private void ButtonBackClick(object sender, EventArgs e)
        {
            buttonNextFinish.Enabled = true;
            _totalFixes = 0;
            _onlyListFixes = true;
            buttonBack.Enabled = false;
            buttonNextFinish.Text = _language.Next;
            groupBox2.Visible = false;
            groupBoxStep1.Visible = true;
            ShowStatus(string.Empty);
            listViewFixes.Items.Clear();
        }

        private void ButtonCancelClick(object sender, EventArgs e)
        {
            SaveConfiguration();
        }

        private void ListViewFixesColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewSorter sorter = (ListViewSorter)listViewFixes.ListViewItemSorter;

            if (e.Column == sorter.ColumnNumber)
            {
                sorter.Descending = !sorter.Descending; // inverse sort direction
            }
            else
            {
                sorter.ColumnNumber = e.Column;
                sorter.Descending = false;
                sorter.IsNumber = e.Column == 1; // only index 1 is numeric
            }
            listViewFixes.Sort();
        }

        private void ButtonSelectAllClick(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.Items)
                item.Checked = true;
        }

        private void ButtonInverseSelectionClick(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.Items)
                item.Checked = !item.Checked;
        }

        private void ListViewFixesSelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewFixes.SelectedItems.Count > 0)
            {
                Paragraph p = (Paragraph) listViewFixes.SelectedItems[0].Tag;
                p = _originalSubtitle.GetFirstParagraphByLineNumber(p.Number);
                if (p != null)
                {
                    int index = _originalSubtitle.GetIndex(p);
                    if (index - 1 > 0)
                        subtitleListView1.EnsureVisible(index - 1);
                    if (index + 1 < subtitleListView1.Items.Count)
                        subtitleListView1.EnsureVisible(index + 1);

                    subtitleListView1.SelectNone();
                    subtitleListView1.Items[index].Selected = true;
                    subtitleListView1.EnsureVisible(index);
                    return;
                }
            }
        }

        private void SubtitleListView1SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_originalSubtitle.Paragraphs.Count > 0)
            {
                int firstSelectedIndex = 0;
                if (subtitleListView1.SelectedItems.Count > 0)
                    firstSelectedIndex = subtitleListView1.SelectedItems[0].Index;

                Paragraph p = GetParagraphOrDefault(firstSelectedIndex);
                if (p != null)
                {
                    textBoxListViewText.TextChanged -= TextBoxListViewTextTextChanged;
                    InitializeListViewEditBox(p);
                    textBoxListViewText.TextChanged += TextBoxListViewTextTextChanged;

                    _subtitleListViewIndex = firstSelectedIndex;
                    UpdateOverlapErrors();
                    UpdateListViewTextInfo(p.Text);
                }
            }
        }

        private void TextBoxListViewTextTextChanged(object sender, EventArgs e)
        {
            if (_subtitleListViewIndex >= 0)
            {
                string text = textBoxListViewText.Text.TrimEnd();
                UpdateListViewTextInfo(text);

                // update _subtitle + listview
                _originalSubtitle.Paragraphs[_subtitleListViewIndex].Text = text;
                subtitleListView1.SetText(_subtitleListViewIndex, text);

                EnableOKButton();
            }
        }

        private void EnableOKButton()
        {
            if (!_hasFixesBeenMade)
            {
                _hasFixesBeenMade = true;
                buttonNextFinish.Enabled = true;
            }
        }

        private Paragraph GetParagraphOrDefault(int index)
        {
            if (_originalSubtitle.Paragraphs == null || _originalSubtitle.Paragraphs.Count <= index || index < 0)
                return null;

            return _originalSubtitle.Paragraphs[index];
        }

        private void InitializeListViewEditBox(Paragraph p)
        {
            textBoxListViewText.TextChanged -= TextBoxListViewTextTextChanged;
            textBoxListViewText.Text = p.Text;
            textBoxListViewText.TextChanged += TextBoxListViewTextTextChanged;

            timeUpDownStartTime.MaskedTextBox.TextChanged -= MaskedTextBox_TextChanged;
            timeUpDownStartTime.TimeCode = p.StartTime;
            timeUpDownStartTime.MaskedTextBox.TextChanged += MaskedTextBox_TextChanged;

            numericUpDownDuration.ValueChanged -= NumericUpDownDurationValueChanged;
            numericUpDownDuration.Value = (decimal)(p.Duration.TotalMilliseconds / 1000.0);
            numericUpDownDuration.ValueChanged += NumericUpDownDurationValueChanged;
        }

        private void NumericUpDownDurationValueChanged(object sender, EventArgs e)
        {
            if (_originalSubtitle.Paragraphs.Count > 0 && subtitleListView1.SelectedItems.Count > 0)
            {
                int firstSelectedIndex = subtitleListView1.SelectedItems[0].Index;

                Paragraph currentParagraph = GetParagraphOrDefault(firstSelectedIndex);
                if (currentParagraph != null)
                {
                    UpdateOverlapErrors();

                    // update _subtitle + listview
                    currentParagraph.EndTime.TotalMilliseconds = currentParagraph.StartTime.TotalMilliseconds + ((double)numericUpDownDuration.Value * 1000.0);
                    subtitleListView1.SetDuration(firstSelectedIndex, currentParagraph);
                }
            }
        }

        private void UpdateOverlapErrors()
        {
            labelStartTimeWarning.Text = string.Empty;
            labelDurationWarning.Text = string.Empty;

            TimeCode startTime = timeUpDownStartTime.TimeCode;
            if (_originalSubtitle.Paragraphs.Count > 0 && subtitleListView1.SelectedItems.Count > 0 && startTime != null)
            {
                int firstSelectedIndex = subtitleListView1.SelectedItems[0].Index;

                Paragraph prevParagraph = GetParagraphOrDefault(firstSelectedIndex - 1);
                if (prevParagraph != null && prevParagraph.EndTime.TotalMilliseconds > startTime.TotalMilliseconds)
                    labelStartTimeWarning.Text = string.Format(_languageGeneral.OverlapPreviousLineX, (prevParagraph.EndTime.TotalMilliseconds - startTime.TotalMilliseconds) / 1000.0);

                Paragraph nextParagraph = GetParagraphOrDefault(firstSelectedIndex + 1);
                if (nextParagraph != null)
                {
                    double durationMilliSeconds = (double)numericUpDownDuration.Value * 1000.0;
                    if (startTime.TotalMilliseconds + durationMilliSeconds > nextParagraph.StartTime.TotalMilliseconds)
                    {
                        labelDurationWarning.Text = string.Format(_languageGeneral.OverlapNextX, ((startTime.TotalMilliseconds + durationMilliSeconds) - nextParagraph.StartTime.TotalMilliseconds) / 1000.0);
                    }

                    if (labelStartTimeWarning.Text.Length == 0 &&
                        startTime.TotalMilliseconds > nextParagraph.StartTime.TotalMilliseconds)
                    {
                        double di = (startTime.TotalMilliseconds - nextParagraph.StartTime.TotalMilliseconds) / 1000.0;
                        labelStartTimeWarning.Text = string.Format(_languageGeneral.OverlapNextX, di);
                    }
                    else if (numericUpDownDuration.Value < 0)
                    {
                        labelDurationWarning.Text = _languageGeneral.Negative;
                    }
                }
            }
        }

        void MaskedTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_subtitleListViewIndex >= 0 &&
                timeUpDownStartTime.TimeCode != null &&
                _originalSubtitle.Paragraphs.Count > 0 &&
                subtitleListView1.SelectedItems.Count > 0)
            {
                TimeCode startTime = timeUpDownStartTime.TimeCode;
                labelStartTimeWarning.Text = string.Empty;
                labelDurationWarning.Text = string.Empty;

                UpdateOverlapErrors();

                // update _subtitle + listview                    
                _originalSubtitle.Paragraphs[_subtitleListViewIndex].EndTime.TotalMilliseconds +=
                    (startTime.TotalMilliseconds - _originalSubtitle.Paragraphs[_subtitleListViewIndex].StartTime.TotalMilliseconds);
                _originalSubtitle.Paragraphs[_subtitleListViewIndex].StartTime = startTime;
                subtitleListView1.SetStartTime(_subtitleListViewIndex, _originalSubtitle.Paragraphs[_subtitleListViewIndex]);
            } 
        }

        private void UpdateListViewTextInfo(string text)
        {
            labelTextLineTotal.Text = string.Empty;

            labelTextLineLengths.Text = _languageGeneral.SingleLineLengths;
            panelSingleLine.Left = labelTextLineLengths.Left + labelTextLineLengths.Width - 6;
            Utilities.DisplayLineLengths(panelSingleLine, text);
            //labelTextLineMaxLength.Text = string.Empty;
            //int maxLineLength = Utilities.GetMaxLineLength(text);
            //labelTextLineMaxLength.Text = string.Format(_languageGeneral.SingleLineMaximumLengthX, maxLineLength);
            //if (maxLineLength > Configuration.Settings.General.SubtitleLineMaximumLength)
            //    labelTextLineMaxLength.ForeColor = System.Drawing.Color.Red;
            //else if (maxLineLength > Configuration.Settings.General.SubtitleLineMaximumLength - 5)
            //    labelTextLineMaxLength.ForeColor = System.Drawing.Color.Orange;
            //else
            //    labelTextLineMaxLength.ForeColor = System.Drawing.Color.Black;

            string s = Utilities.RemoveHtmlTags(text).Replace(Environment.NewLine, " ");
            if (s.Length < Configuration.Settings.General.SubtitleLineMaximumLength * 1.9)
            {
                labelTextLineTotal.ForeColor = System.Drawing.Color.Black;
                labelTextLineTotal.Text = string.Format(_languageGeneral.TotalLengthX, s.Length);
            }
            else if (s.Length < Configuration.Settings.General.SubtitleLineMaximumLength * 2.1)
            {
                labelTextLineTotal.ForeColor = System.Drawing.Color.Orange;
                labelTextLineTotal.Text = string.Format(_languageGeneral.TotalLengthX, s.Length);
            }
            else
            {
                labelTextLineTotal.ForeColor = System.Drawing.Color.Red;
                labelTextLineTotal.Text = string.Format(_languageGeneral.TotalLengthXSplitLine, s.Length);
            }
        }

        private void ButtonFixesSelectAllClick(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewFixes.Items)
                item.Checked = true;
        }

        private void ButtonFixesInverseClick(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewFixes.Items)
                item.Checked = !item.Checked;
        }

        private void ButtonFixesApplyClick(object sender, EventArgs e)
        {
            _hasFixesBeenMade = true;
            Cursor = Cursors.WaitCursor;
            ShowStatus(_language.Analysing);

            _subtitleListViewIndex = -1;
            int firstSelectedIndex = 0;
            if (subtitleListView1.SelectedItems.Count > 0)
                firstSelectedIndex = subtitleListView1.SelectedItems[0].Index;

            _numberOfImportantLogMessages = 0;
            _onlyListFixes = false;
            _totalFixes = 0;
            _totalErrors = 0;
            RunSelectedActions();
            _originalSubtitle = new Subtitle(_subtitle);
            subtitleListView1.Fill(_originalSubtitle);
            RefreshFixes();
            if (listViewFixes.Items.Count == 0)
            {
                subtitleListView1.SelectIndexAndEnsureVisible(firstSelectedIndex);
            }
            if (_totalFixes == 0 && _totalErrors == 0)
                ShowStatus(_language.NothingToFix);
            else if (_totalFixes > 0)
                ShowStatus(string.Format(_language.XFixesApplied, _totalFixes));
            else if (_totalErrors > 0)
                ShowStatus(_language.NothingToFixBut);

            Cursor = Cursors.Default;
            if (_numberOfImportantLogMessages == 0)
                labelNumberOfImportantLogMessages.Text = string.Empty;
            else
                labelNumberOfImportantLogMessages.Text = string.Format("{0} important log messages!", _numberOfImportantLogMessages);
        }

        private void ButtonRefreshFixesClick(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            ShowStatus(_language.Analysing);
            _totalFixes = 0;
            RefreshFixes();

            ShowAvailableFixesStatus();

            Cursor = Cursors.Default;
        }

        private void ShowAvailableFixesStatus()
        {
            if (_totalFixes == 0 && _totalErrors == 0)
            {
                ShowStatus(_language.NothingToFix);
                if (subtitleListView1.SelectedItems.Count == 0)
                    subtitleListView1.SelectIndexAndEnsureVisible(0);
            }
            else if (_totalFixes > 0)
                ShowStatus(string.Format(_language.FixesFoundX, _totalFixes));
            else if (_totalErrors > 0)
                ShowStatus(_language.NothingToFixBut);
        }

        private void RefreshFixes()
        {
            // save de-seleced fixes
            List<string> deSelectedFixes = new List<string>();
            foreach (ListViewItem item in listViewFixes.Items)
            {
                if (!item.Checked)
                    deSelectedFixes.Add(item.SubItems[1].Text + item.SubItems[2].Text + item.SubItems[3].Text);
            }

            listViewFixes.Items.Clear();
            _onlyListFixes = true;
            Next();

            // restore de-selected fixes
            foreach (ListViewItem item in listViewFixes.Items)
            {
                if  (deSelectedFixes.Contains(item.SubItems[1].Text + item.SubItems[2].Text + item.SubItems[3].Text))
                    item.Checked = false;
            }
        }

        private void ButtonAutoBreakClick(object sender, EventArgs e)
        {
            if (textBoxListViewText.Text.Length > 0)
            {
                string oldText = textBoxListViewText.Text;
                textBoxListViewText.Text = Utilities.AutoBreakLine(textBoxListViewText.Text);
                if (oldText != textBoxListViewText.Text)
                    EnableOKButton();
            }
        }

        private void ButtonUnBreakClick(object sender, EventArgs e)
        {
            string oldText = textBoxListViewText.Text;
            textBoxListViewText.Text = Utilities.UnbreakLine(textBoxListViewText.Text);
            if (oldText != textBoxListViewText.Text)
                EnableOKButton();
        }

        private void ToolStripMenuItemDeleteClick(object sender, EventArgs e)
        {
            if (_originalSubtitle.Paragraphs.Count > 0 && subtitleListView1.SelectedItems.Count > 0)
            {
                _subtitleListViewIndex = -1;

                var indexes = new List<int>();
                foreach (ListViewItem item in subtitleListView1.SelectedItems)
                    indexes.Add(item.Index);
                int firstIndex = subtitleListView1.SelectedItems[0].Index;

                int startNumber = _originalSubtitle.Paragraphs[0].Number;
                if (startNumber == 2)
                    startNumber = 1;

                // save de-seleced fixes
                List<string> deSelectedFixes = new List<string>();
                foreach (ListViewItem item in listViewFixes.Items)
                {
                    if (!item.Checked)
                    {
                        int number = Convert.ToInt32(item.SubItems[1].Text);
                        if (number > firstIndex)
                            number -= subtitleListView1.SelectedItems.Count;
                        deSelectedFixes.Add(number + item.SubItems[2].Text + item.SubItems[3].Text);
                    }
                }

                indexes.Reverse();
                foreach (int i in indexes)
                {
                    _originalSubtitle.Paragraphs.RemoveAt(i);
                }
                _originalSubtitle.Renumber(startNumber);
                subtitleListView1.Fill(_originalSubtitle);
                if (subtitleListView1.Items.Count > firstIndex)
                {
                    subtitleListView1.Items[firstIndex].Selected = true;
                }
                else if (subtitleListView1.Items.Count > 0)
                {
                    subtitleListView1.Items[subtitleListView1.Items.Count - 1].Selected = true;
                }

                // refresh fixes
                listViewFixes.Items.Clear();
                _onlyListFixes = true;
                Next();

                // restore de-selected fixes
                foreach (ListViewItem item in listViewFixes.Items)
                {
                    if (deSelectedFixes.Contains(item.SubItems[1].Text + item.SubItems[2].Text + item.SubItems[3].Text))
                        item.Checked = false;
                }

            }
        }

        private void MergeSelectedLinesToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_originalSubtitle.Paragraphs.Count > 0 && subtitleListView1.SelectedItems.Count > 0)
            {
                int startNumber = _originalSubtitle.Paragraphs[0].Number;
                int firstSelectedIndex = subtitleListView1.SelectedItems[0].Index;

                // save de-seleced fixes
                List<string> deSelectedFixes = new List<string>();
                foreach (ListViewItem item in listViewFixes.Items)
                {
                    if (!item.Checked)
                    {
                        int firstSelectedNumber = subtitleListView1.GetSelectedParagraph(_originalSubtitle).Number;
                        int number = Convert.ToInt32(item.SubItems[1].Text);
                        if (number > firstSelectedNumber)
                            number--;
                        deSelectedFixes.Add(number + item.SubItems[2].Text + item.SubItems[3].Text);
                    }
                }

                Paragraph currentParagraph = _originalSubtitle.GetParagraphOrDefault(firstSelectedIndex);
                Paragraph nextParagraph = _originalSubtitle.GetParagraphOrDefault(firstSelectedIndex + 1);

                if (nextParagraph != null && currentParagraph != null)
                {
                    subtitleListView1.SelectedIndexChanged -= SubtitleListView1SelectedIndexChanged;

                    currentParagraph.Text = currentParagraph.Text.Replace(Environment.NewLine, " ");
                    currentParagraph.Text += Environment.NewLine + nextParagraph.Text.Replace(Environment.NewLine, " ");
                    currentParagraph.EndTime = nextParagraph.EndTime;

                    _originalSubtitle.Paragraphs.Remove(nextParagraph);

                    _originalSubtitle.Renumber(startNumber);
                    subtitleListView1.Fill(_originalSubtitle);
                    subtitleListView1.SelectIndexAndEnsureVisible(firstSelectedIndex);
                    subtitleListView1.SelectedIndexChanged += SubtitleListView1SelectedIndexChanged;
                    _subtitleListViewIndex = -1;
                    SubtitleListView1SelectedIndexChanged(null, null);
                }

                // refresh fixes
                listViewFixes.Items.Clear();
                _onlyListFixes = true;
                Next();

                // restore de-selected fixes
                foreach (ListViewItem item in listViewFixes.Items)
                {
                    if (deSelectedFixes.Contains(item.SubItems[1].Text + item.SubItems[2].Text + item.SubItems[3].Text))
                        item.Checked = false;
                }
            }
        }

        private void ContextMenuStripListviewOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (subtitleListView1.SelectedItems.Count == 0)
            {
                e.Cancel = true;
            }
            else if (subtitleListView1.SelectedItems.Count == 2 &&
                     subtitleListView1.SelectedItems[0].Index == subtitleListView1.SelectedItems[1].Index - 1)
            {

                mergeSelectedLinesToolStripMenuItem.Visible = true;
                toolStripSeparator1.Visible = true;
            }
            else
            {
                mergeSelectedLinesToolStripMenuItem.Visible = false;
                toolStripSeparator1.Visible = false;
            }
        }

        private void FixCommonErrors_Resize(object sender, EventArgs e)
        {
            groupBox2.Width = this.Width - (groupBox2.Left * 2 + 15);
            groupBoxStep1.Width = this.Width - (groupBoxStep1.Left * 2 + 15);
            buttonCancel.Left = this.Width - (buttonCancel.Width + 26);
            buttonNextFinish.Left = buttonCancel.Left - (buttonNextFinish.Width + 6);
            buttonBack.Left = buttonNextFinish.Left - (buttonBack.Width + 6);
            tabControl1.Width = groupBox2.Width - (tabControl1.Left * 2);
            listView1.Width = groupBoxStep1.Width - (listView1.Left * 2);
        }

        private void FixCommonErrors_Shown(object sender, EventArgs e)
        {
            FixCommonErrors_Resize(null, null);
        }

    }
}