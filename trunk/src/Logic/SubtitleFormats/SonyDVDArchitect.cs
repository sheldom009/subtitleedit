﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    public class SonyDVDArchitect : SubtitleFormat
    {
        public override string Extension
        {
            get { return ".sub"; }
        }

        public override string Name
        {
            get { return "Sony DVDArchitect"; }
        }

        public override bool HasLineNumber
        {
            get { return false; }
        }

        public override bool IsTimeBased
        {
            get { return true; }
        }

        public override bool IsMine(List<string> lines, string fileName)
        {
            var subtitle = new Subtitle();
            LoadSubtitle(subtitle, lines, fileName);
            return subtitle.Paragraphs.Count > _errorCount;
        }

        public override string ToText(Subtitle subtitle, string title)
        {
            var sb = new StringBuilder();
            foreach (Paragraph p in subtitle.Paragraphs)
            { 
                string text = Utilities.RemoveHtmlTags(p.Text);
                text = text.Replace(Environment.NewLine, string.Empty);
                sb.AppendLine(string.Format("{0:00}:{1:00}:{2:00}:{3:00} - {4:00}:{5:00}:{6:00}:{7:00}  \t{8:00}",
                                            p.StartTime.Hours, p.StartTime.Minutes, p.StartTime.Seconds, p.StartTime.Milliseconds / 10,
                                            p.EndTime.Hours, p.EndTime.Minutes, p.EndTime.Seconds, p.EndTime.Milliseconds / 10,
                                            text));
            }
            return sb.ToString().Trim();
        }

        public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        {   // 00:04:10:92 - 00:04:13:32  	Raise Yourself To Help Mankind
            // 00:04:27:92 - 00:04:30:92  	الجهة المتولية للمسئولية الاجتماعية لشركتنا.

            var regex = new Regex(@"^\d\d:\d\d:\d\d:\d\d[ ]+-[ ]+\d\d:\d\d:\d\d:\d\d", RegexOptions.Compiled); 
            _errorCount = 0;
            foreach (string line in lines)
            {
                if (line.Trim().Length > 0)
                {
                    bool success = false;
                    var match = regex.Match(line);
                    if (line.Length > 26 && match.Success)
                    {
                        string s = line.Substring(0, match.Length);
                        s = s.Replace(" - ", ":");
                        s = s.Replace(" ", string.Empty);
                        string[] parts = s.Split(':');
                        if (parts.Length ==  8)
                        {
                            int hours = int.Parse(parts[0]);
                            int minutes = int.Parse(parts[1]);
                            int seconds = int.Parse(parts[2]);
                            int milliseconds = int.Parse(parts[3]) * 10;
                            var start = new TimeCode(hours, minutes, seconds, milliseconds);

                            hours = int.Parse(parts[4]);
                            minutes = int.Parse(parts[5]);
                            seconds = int.Parse(parts[6]);
                            milliseconds = int.Parse(parts[7]) * 10;
                            var end = new TimeCode(hours, minutes, seconds, milliseconds);

                            string text = line.Substring(match.Length).TrimStart();
                            text = text.Replace("|", Environment.NewLine);
                            
                            var p = new Paragraph(start, end, text);
                            subtitle.Paragraphs.Add(p);
                            success = true;
                        }
                    }
                    if (!success)
                        _errorCount++;
                }
            }
            subtitle.Renumber(1);
        }
    }
}
